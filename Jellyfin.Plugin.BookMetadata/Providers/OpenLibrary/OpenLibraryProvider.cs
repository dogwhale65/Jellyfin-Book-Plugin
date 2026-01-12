using System;
using System.Collections.Generic;
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

namespace Jellyfin.Plugin.BookMetadata.Providers.OpenLibrary;

/// <summary>
/// Open Library metadata provider.
/// </summary>
public class OpenLibraryProvider : IRemoteMetadataProvider<Book, BookInfo>
{
    private readonly ILogger<OpenLibraryProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenLibraryClient _client;
    private readonly MetadataCache _cache;
    private readonly RateLimiter _rateLimiter;
    private readonly FuzzyMatcher _fuzzyMatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenLibraryProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public OpenLibraryProvider(
        ILogger<OpenLibraryProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _client = new OpenLibraryClient(httpClientFactory, logger);

        var config = Plugin.Instance?.Configuration;
        _cache = new MetadataCache(config?.CacheDuration ?? 24);
        _rateLimiter = new RateLimiter(config?.OpenLibraryRateLimit ?? 100);
        _fuzzyMatcher = new FuzzyMatcher(config?.FuzzyMatchThreshold ?? 85);
    }

    /// <inheritdoc />
    public string Name => "Open Library";

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
        BookInfo searchInfo,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config?.EnableOpenLibrary != true)
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

            var results = new List<RemoteSearchResult>();

            // Try ISBN search first (most accurate)
            var isbn = searchInfo.GetProviderId("ISBN");
            if (!string.IsNullOrEmpty(isbn) && config?.EnableISBNExtraction == true)
            {
                _logger.LogDebug("Searching Open Library by ISBN: {ISBN}", isbn);
                var booksResponse = await _client.SearchByISBNAsync(isbn, cancellationToken).ConfigureAwait(false);

                if (booksResponse != null && booksResponse.Count > 0)
                {
                    foreach (var kvp in booksResponse)
                    {
                        var result = ConvertBookToSearchResult(kvp.Value, kvp.Key);
                        if (result != null)
                        {
                            results.Add(result);
                        }
                    }
                }
            }

            // Fall back to title/author search
            if (results.Count == 0 && !string.IsNullOrEmpty(searchInfo.Name))
            {
                _logger.LogDebug("Searching Open Library by title/author: {Title}", searchInfo.Name);

                string? author = null;
                if (searchInfo.Name.Contains(" by ", StringComparison.OrdinalIgnoreCase))
                {
                    author = searchInfo.Name.Split(" by ", StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                }

                var searchResponse = await _client.SearchByTitleAuthorAsync(
                    searchInfo.Name,
                    author,
                    cancellationToken).ConfigureAwait(false);

                if (searchResponse?.Docs != null && searchResponse.Docs.Count > 0)
                {
                    results.AddRange(searchResponse.Docs
                        .Where(doc => doc != null)
                        .Select(doc => ConvertSearchDocToResult(doc))
                        .Where(result => result != null)
                        .Cast<RemoteSearchResult>());
                }
            }

            if (results.Count == 0)
            {
                _logger.LogInformation("No results found in Open Library for: {Title}", searchInfo.Name);
                return Enumerable.Empty<RemoteSearchResult>();
            }

            // Score and filter results
            var scoredResults = results
                .Select(result => new
                {
                    Result = result,
                    Score = config?.EnableFuzzyMatching == true
                        ? _fuzzyMatcher.GetMatchScore(
                            searchInfo.Name,
                            result.Name,
                            searchInfo.Year,
                            result.ProductionYear)
                        : 100
                })
                .Where(x => x.Score >= (config?.FuzzyMatchThreshold ?? 85))
                .OrderByDescending(x => x.Score)
                .Select(x => x.Result)
                .Take(10)
                .ToList();

            _cache.Set(cacheKey, scoredResults);
            _logger.LogDebug("Found {Count} results for {Title}", scoredResults.Count, searchInfo.Name);

            return scoredResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Open Library for {Title}", searchInfo.Name);
            return Enumerable.Empty<RemoteSearchResult>();
        }
    }

    /// <inheritdoc />
    public async Task<MetadataResult<Book>> GetMetadata(
        BookInfo info,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config?.EnableOpenLibrary != true)
        {
            return new MetadataResult<Book>();
        }

        try
        {
            OpenLibraryBook? book = null;

            // Check if we have an ISBN
            var isbn = info.GetProviderId("ISBN");
            if (!string.IsNullOrEmpty(isbn))
            {
                await _rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
                var booksResponse = await _client.SearchByISBNAsync(isbn, cancellationToken).ConfigureAwait(false);

                if (booksResponse != null && booksResponse.Count > 0)
                {
                    book = booksResponse.Values.FirstOrDefault();
                }
            }

            // If no book found, search by title/author
            if (book == null && !string.IsNullOrEmpty(info.Name))
            {
                var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                var firstResult = searchResults.FirstOrDefault();

                if (firstResult != null && !string.IsNullOrEmpty(firstResult.ProviderIds.GetValueOrDefault("ISBN")))
                {
                    await _rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
                    var booksResponse = await _client.SearchByISBNAsync(
                        firstResult.ProviderIds["ISBN"],
                        cancellationToken).ConfigureAwait(false);

                    if (booksResponse != null && booksResponse.Count > 0)
                    {
                        book = booksResponse.Values.FirstOrDefault();
                    }
                }
            }

            if (book == null)
            {
                return new MetadataResult<Book>();
            }

            var bookItem = MapBookToBook(book);

            return new MetadataResult<Book>
            {
                Item = bookItem,
                HasMetadata = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata from Open Library for {Title}", info.Name);
            return new MetadataResult<Book>();
        }
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        return httpClient.GetAsync(url, cancellationToken);
    }

    private RemoteSearchResult? ConvertBookToSearchResult(OpenLibraryBook book, string bibkey)
    {
        if (book == null || string.IsNullOrEmpty(book.Title))
        {
            return null;
        }

        var result = new RemoteSearchResult
        {
            Name = book.Title,
            SearchProviderName = Name
        };

        // Add subtitle
        if (!string.IsNullOrEmpty(book.Subtitle))
        {
            result.Name += ": " + book.Subtitle;
        }

        // Set overview from excerpts
        if (book.Excerpts != null && book.Excerpts.Count > 0)
        {
            result.Overview = book.Excerpts[0].Text;
        }

        // Parse publication year
        if (!string.IsNullOrEmpty(book.PublishDate))
        {
            if (DateTime.TryParse(book.PublishDate, out var publishDate))
            {
                result.ProductionYear = publishDate.Year;
                result.PremiereDate = publishDate;
            }
        }

        // Set image URL
        if (book.Cover != null)
        {
            result.ImageUrl = book.Cover.Large ?? book.Cover.Medium ?? book.Cover.Small;
        }

        // Add provider IDs
        if (book.Url != null)
        {
            result.SetProviderId("OpenLibraryId", book.Url);
        }

        // Add ISBN
        var isbn = book.Identifiers?.Isbn13?.FirstOrDefault() ?? book.Identifiers?.Isbn10?.FirstOrDefault();
        if (!string.IsNullOrEmpty(isbn))
        {
            result.SetProviderId("ISBN", isbn);
        }

        return result;
    }

    private RemoteSearchResult? ConvertSearchDocToResult(OpenLibrarySearchDoc doc)
    {
        if (doc == null || string.IsNullOrEmpty(doc.Title))
        {
            return null;
        }

        var result = new RemoteSearchResult
        {
            Name = doc.Title,
            SearchProviderName = Name
        };

        // Add subtitle
        if (!string.IsNullOrEmpty(doc.Subtitle))
        {
            result.Name += ": " + doc.Subtitle;
        }

        // Set publication year
        if (doc.FirstPublishYear.HasValue)
        {
            result.ProductionYear = doc.FirstPublishYear.Value;
        }

        // Set image URL
        if (doc.CoverId.HasValue)
        {
            result.ImageUrl = $"https://covers.openlibrary.org/b/id/{doc.CoverId.Value}-L.jpg";
        }

        // Add provider IDs
        if (!string.IsNullOrEmpty(doc.Key))
        {
            result.SetProviderId("OpenLibraryId", doc.Key);
        }

        // Add ISBN
        var isbn = doc.Isbn?.FirstOrDefault();
        if (!string.IsNullOrEmpty(isbn))
        {
            result.SetProviderId("ISBN", isbn);
        }

        return result;
    }

    private Book MapBookToBook(OpenLibraryBook book)
    {
        var bookItem = new Book
        {
            Name = book.Title ?? "Unknown"
        };

        // Add subtitle
        if (!string.IsNullOrEmpty(book.Subtitle))
        {
            bookItem.Name += ": " + book.Subtitle;
        }

        // Set overview from excerpts
        if (book.Excerpts != null && book.Excerpts.Count > 0)
        {
            bookItem.Overview = string.Join("\n\n", book.Excerpts.Select(e => e.Text));
        }

        // Set publisher
        if (book.Publishers != null && book.Publishers.Count > 0)
        {
            bookItem.Studios = book.Publishers.Select(p => p.Name).Where(n => !string.IsNullOrEmpty(n)).ToArray()!;
        }

        // Parse publication date
        if (!string.IsNullOrEmpty(book.PublishDate))
        {
            if (DateTime.TryParse(book.PublishDate, out var publishDate))
            {
                bookItem.PremiereDate = publishDate;
                bookItem.ProductionYear = publishDate.Year;
            }
        }

        // Add authors
        // Note: Author info stored in metadata, not People collection

        // Add genres/subjects
        if (book.Subjects != null && book.Subjects.Count > 0)
        {
            bookItem.Genres = book.Subjects
                .Select(s => s.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToArray()!;
        }

        // Add provider IDs
        if (!string.IsNullOrEmpty(book.Url))
        {
            bookItem.SetProviderId("OpenLibraryId", book.Url);
        }

        // Add ISBN
        var isbn = book.Identifiers?.Isbn13?.FirstOrDefault() ?? book.Identifiers?.Isbn10?.FirstOrDefault();
        if (!string.IsNullOrEmpty(isbn))
        {
            bookItem.SetProviderId("ISBN", isbn);
        }

        return bookItem;
    }
}
