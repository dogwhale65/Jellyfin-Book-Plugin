using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.BookMetadata.Providers.GoogleBooks;

/// <summary>
/// HTTP client for Google Books API.
/// </summary>
public class GoogleBooksClient
{
    private const string BaseUrl = "https://www.googleapis.com/books/v1/volumes";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBooksClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public GoogleBooksClient(
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Searches for books by ISBN.
    /// </summary>
    /// <param name="isbn">The ISBN to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Google Books API response.</returns>
    public async Task<GoogleBooksResponse?> SearchByISBNAsync(
        string isbn,
        CancellationToken cancellationToken)
    {
        var query = $"isbn:{isbn}";
        return await SearchAsync(query, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches for books by title and author.
    /// </summary>
    /// <param name="title">The title to search for.</param>
    /// <param name="author">The author to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Google Books API response.</returns>
    public async Task<GoogleBooksResponse?> SearchByTitleAuthorAsync(
        string? title,
        string? author,
        CancellationToken cancellationToken)
    {
        var queryParts = new System.Collections.Generic.List<string>();

        if (!string.IsNullOrEmpty(title))
        {
            queryParts.Add($"intitle:{title}");
        }

        if (!string.IsNullOrEmpty(author))
        {
            queryParts.Add($"inauthor:{author}");
        }

        if (queryParts.Count == 0)
        {
            return null;
        }

        var query = string.Join("+", queryParts);
        return await SearchAsync(query, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a volume by ID.
    /// </summary>
    /// <param name="volumeId">The volume ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The volume.</returns>
    public async Task<GoogleBooksVolume?> GetVolumeAsync(
        string volumeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var url = $"{BaseUrl}/{HttpUtility.UrlEncode(volumeId)}";
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google Books API returned status code {StatusCode} for volume {VolumeId}",
                    response.StatusCode,
                    volumeId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<GoogleBooksVolume>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching volume {VolumeId} from Google Books", volumeId);
            return null;
        }
    }

    private async Task<GoogleBooksResponse?> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var url = $"{BaseUrl}?q={HttpUtility.UrlEncode(query)}&maxResults=10";
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Google Books API returned status code {StatusCode} for query: {Query}",
                    response.StatusCode,
                    query);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<GoogleBooksResponse>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Google Books with query: {Query}", query);
            return null;
        }
    }
}
