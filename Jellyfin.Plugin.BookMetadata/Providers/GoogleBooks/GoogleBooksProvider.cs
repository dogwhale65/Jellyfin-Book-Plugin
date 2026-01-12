using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.BookMetadata.Utils;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.BookMetadata.Providers.GoogleBooks;

/// <summary>
/// Google Books metadata provider.
/// </summary>
public class GoogleBooksProvider : IRemoteMetadataProvider<Book, BookInfo>
{
    private readonly ILogger<GoogleBooksProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleBooksClient _client;
    private readonly MetadataCache _cache;
    private readonly RateLimiter _rateLimiter;
    private readonly FuzzyMatcher _fuzzyMatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBooksProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public GoogleBooksProvider(
        ILogger<GoogleBooksProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _client = new GoogleBooksClient(httpClientFactory, logger);

        var config = Plugin.Instance?.Configuration;
        _cache = new MetadataCache(config?.CacheDuration ?? 24);
        _rateLimiter = new RateLimiter(config?.GoogleBooksRateLimit ?? 10);
        _fuzzyMatcher = new FuzzyMatcher(config?.FuzzyMatchThreshold ?? 85);
    }

    /// <inheritdoc />
    public string Name => "Google Books";

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
        BookInfo searchInfo,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config?.EnableGoogleBooks != true)
        {
            return Enumerable.Empty<RemoteSearchResult>();
        }

        try
        {
            // Check cache first
            var cacheKey = MetadataCache.GenerateKey(
                Name,
                searchInfo.GetProviderId("ISBN") ?? searchInfo.Name ?? string.Empty);
            var cached = _cache.Get<List<RemoteSearchResult>>(cacheKey);
            if (cached != null)
            {
                _logger.LogDebug("Returning cached results for {Key}", cacheKey);
                return cached;
            }

            // Apply rate limiting
            await _rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);

            GoogleBooksResponse? response = null;

            // Try ISBN search first (most accurate)
            var isbn = searchInfo.GetProviderId("ISBN");
            if (!string.IsNullOrEmpty(isbn) && config?.EnableISBNExtraction == true)
            {
                _logger.LogDebug("Searching Google Books by ISBN: {ISBN}", isbn);
                response = await _client.SearchByISBNAsync(isbn, cancellationToken).ConfigureAwait(false);
            }

            // Fall back to title/author search
            if ((response == null || response.TotalItems == 0) && !string.IsNullOrEmpty(searchInfo.Name))
            {
                _logger.LogDebug("Searching Google Books by title/author: {Title}", searchInfo.Name);

                // Extract author from metadata if available
                string? author = null;
                if (searchInfo.MetadataCountryCode != null && searchInfo.MetadataLanguage != null)
                {
                    // Try to get author from search info
                    author = searchInfo.Name.Contains(" by ", StringComparison.OrdinalIgnoreCase)
                        ? searchInfo.Name.Split(" by ", StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                        : null;
                }

                response = await _client.SearchByTitleAuthorAsync(
                    searchInfo.Name,
                    author,
                    cancellationToken).ConfigureAwait(false);
            }

            if (response == null || response.Items == null || response.Items.Count == 0)
            {
                _logger.LogInformation("No results found in Google Books for: {Title}", searchInfo.Name);
                return Enumerable.Empty<RemoteSearchResult>();
            }

            // Convert to RemoteSearchResult and score
            var results = response.Items
                .Where(item => item.VolumeInfo != null)
                .Select(item => ConvertToSearchResult(item))
                .Where(result => result != null)
                .Select(result => new
                {
                    Result = result!,
                    Score = config?.EnableFuzzyMatching == true
                        ? _fuzzyMatcher.GetMatchScore(
                            searchInfo.Name,
                            result!.Name,
                            searchInfo.Year,
                            result.ProductionYear)
                        : 100 // If fuzzy matching disabled, accept all
                })
                .Where(x => x.Score >= (config?.FuzzyMatchThreshold ?? 85))
                .OrderByDescending(x => x.Score)
                .Select(x => x.Result)
                .Take(10)
                .ToList();

            _cache.Set(cacheKey, results);
            _logger.LogDebug("Found {Count} results for {Title}", results.Count, searchInfo.Name);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Google Books for {Title}", searchInfo.Name);
            return Enumerable.Empty<RemoteSearchResult>();
        }
    }

    /// <inheritdoc />
    public async Task<MetadataResult<Book>> GetMetadata(
        BookInfo info,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config?.EnableGoogleBooks != true)
        {
            return new MetadataResult<Book>();
        }

        try
        {
            GoogleBooksVolume? volume = null;

            // Check if we have a Google Books ID
            var googleBooksId = info.GetProviderId("GoogleBooksId");
            if (!string.IsNullOrEmpty(googleBooksId))
            {
                await _rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
                volume = await _client.GetVolumeAsync(googleBooksId, cancellationToken).ConfigureAwait(false);
            }

            // If no ID or fetch failed, search for the book
            if (volume == null)
            {
                var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                var firstResult = searchResults.FirstOrDefault();

                if (firstResult != null && !string.IsNullOrEmpty(firstResult.ProviderIds.GetValueOrDefault("GoogleBooksId")))
                {
                    await _rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
                    volume = await _client.GetVolumeAsync(
                        firstResult.ProviderIds["GoogleBooksId"],
                        cancellationToken).ConfigureAwait(false);
                }
            }

            if (volume?.VolumeInfo == null)
            {
                return new MetadataResult<Book>();
            }

            var book = MapVolumeToBook(volume);

            return new MetadataResult<Book>
            {
                Item = book,
                HasMetadata = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata from Google Books for {Title}", info.Name);
            return new MetadataResult<Book>();
        }
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        return httpClient.GetAsync(url, cancellationToken);
    }

    private RemoteSearchResult? ConvertToSearchResult(GoogleBooksVolume volume)
    {
        if (volume.VolumeInfo == null)
        {
            return null;
        }

        var volumeInfo = volume.VolumeInfo;
        var result = new RemoteSearchResult
        {
            Name = volumeInfo.Title ?? "Unknown",
            SearchProviderName = Name
        };

        // Add subtitle if present
        if (!string.IsNullOrEmpty(volumeInfo.Subtitle))
        {
            result.Name += ": " + volumeInfo.Subtitle;
        }

        // Set overview
        result.Overview = volumeInfo.Description;

        // Parse publication year
        if (!string.IsNullOrEmpty(volumeInfo.PublishedDate))
        {
            if (DateTime.TryParse(volumeInfo.PublishedDate, out var publishDate))
            {
                result.ProductionYear = publishDate.Year;
                result.PremiereDate = publishDate;
            }
        }

        // Set image URL
        if (volumeInfo.ImageLinks != null)
        {
            result.ImageUrl = volumeInfo.ImageLinks.ExtraLarge
                ?? volumeInfo.ImageLinks.Large
                ?? volumeInfo.ImageLinks.Medium
                ?? volumeInfo.ImageLinks.Small
                ?? volumeInfo.ImageLinks.Thumbnail;

            // Ensure HTTPS
            if (!string.IsNullOrEmpty(result.ImageUrl) && result.ImageUrl.StartsWith("http:", StringComparison.OrdinalIgnoreCase))
            {
                result.ImageUrl = "https:" + result.ImageUrl[5..];
            }
        }

        // Add provider IDs
        result.SetProviderId("GoogleBooksId", volume.Id);

        // Add ISBN
        var isbn = volumeInfo.IndustryIdentifiers?
            .FirstOrDefault(x => x.Type?.Contains("ISBN", StringComparison.OrdinalIgnoreCase) == true);
        if (isbn != null && !string.IsNullOrEmpty(isbn.Identifier))
        {
            result.SetProviderId("ISBN", isbn.Identifier);
        }

        return result;
    }

    private Book MapVolumeToBook(GoogleBooksVolume volume)
    {
        var volumeInfo = volume.VolumeInfo!;
        var book = new Book
        {
            Name = volumeInfo.Title ?? "Unknown"
        };

        // Add subtitle
        if (!string.IsNullOrEmpty(volumeInfo.Subtitle))
        {
            book.Name += ": " + volumeInfo.Subtitle;
        }

        // Set overview
        book.Overview = volumeInfo.Description;

        // Set publisher
        if (!string.IsNullOrEmpty(volumeInfo.Publisher))
        {
            book.Studios = new[] { volumeInfo.Publisher };
        }

        // Parse publication date
        if (!string.IsNullOrEmpty(volumeInfo.PublishedDate))
        {
            if (DateTime.TryParse(volumeInfo.PublishedDate, out var publishDate))
            {
                book.PremiereDate = publishDate;
                book.ProductionYear = publishDate.Year;
            }
        }

        // Add authors
        // Note: Author info stored in metadata, not People collection

        // Add genres/categories
        if (volumeInfo.Categories != null && volumeInfo.Categories.Count > 0)
        {
            book.Genres = volumeInfo.Categories.ToArray();
        }

        // Set community rating (convert from 5-point to 10-point scale)
        if (volumeInfo.AverageRating.HasValue)
        {
            book.CommunityRating = (float)(volumeInfo.AverageRating.Value * 2);
        }

        // Set language
        if (!string.IsNullOrEmpty(volumeInfo.Language))
        {
            book.OfficialRating = volumeInfo.Language.ToUpperInvariant();
        }

        // Add provider IDs
        book.SetProviderId("GoogleBooksId", volume.Id);

        // Add ISBN
        var isbn = volumeInfo.IndustryIdentifiers?
            .FirstOrDefault(x => x.Type?.Contains("ISBN_13", StringComparison.OrdinalIgnoreCase) == true)
            ?? volumeInfo.IndustryIdentifiers?
                .FirstOrDefault(x => x.Type?.Contains("ISBN", StringComparison.OrdinalIgnoreCase) == true);

        if (isbn != null && !string.IsNullOrEmpty(isbn.Identifier))
        {
            book.SetProviderId("ISBN", isbn.Identifier);
        }

        return book;
    }
}
