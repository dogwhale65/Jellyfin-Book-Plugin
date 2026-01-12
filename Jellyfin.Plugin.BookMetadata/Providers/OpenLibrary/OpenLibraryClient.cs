using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.BookMetadata.Providers.OpenLibrary;

/// <summary>
/// HTTP client for Open Library API.
/// </summary>
public class OpenLibraryClient
{
    private const string SearchApiUrl = "https://openlibrary.org/search.json";
    private const string BooksApiUrl = "https://openlibrary.org/api/books";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenLibraryClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public OpenLibraryClient(
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Searches for books by ISBN using the Books API.
    /// </summary>
    /// <param name="isbn">The ISBN to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Open Library Books API response.</returns>
    public async Task<OpenLibraryBooksResponse?> SearchByISBNAsync(
        string isbn,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var url = $"{BooksApiUrl}?bibkeys=ISBN:{HttpUtility.UrlEncode(isbn)}&format=json&jscmd=data";
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Open Library Books API returned status code {StatusCode} for ISBN {ISBN}",
                    response.StatusCode,
                    isbn);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<OpenLibraryBooksResponse>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Open Library Books API for ISBN: {ISBN}", isbn);
            return null;
        }
    }

    /// <summary>
    /// Searches for books by title and author using the Search API.
    /// </summary>
    /// <param name="title">The title to search for.</param>
    /// <param name="author">The author to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Open Library Search API response.</returns>
    public async Task<OpenLibrarySearchResponse?> SearchByTitleAuthorAsync(
        string? title,
        string? author,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var queryParams = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(title))
            {
                queryParams.Add($"title={HttpUtility.UrlEncode(title)}");
            }

            if (!string.IsNullOrEmpty(author))
            {
                queryParams.Add($"author={HttpUtility.UrlEncode(author)}");
            }

            if (queryParams.Count == 0)
            {
                return null;
            }

            var url = $"{SearchApiUrl}?{string.Join("&", queryParams)}&limit=10";
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Open Library Search API returned status code {StatusCode}",
                    response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<OpenLibrarySearchResponse>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Open Library for title: {Title}, author: {Author}", title, author);
            return null;
        }
    }
}
