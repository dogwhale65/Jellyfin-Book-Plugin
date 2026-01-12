using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.BookMetadata.Providers.OpenLibrary;

/// <summary>
/// Open Library image provider.
/// </summary>
public class OpenLibraryImageProvider : IRemoteImageProvider
{
    private readonly ILogger<OpenLibraryImageProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenLibraryImageProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public OpenLibraryImageProvider(
        ILogger<OpenLibraryImageProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public string Name => "Open Library";

    /// <inheritdoc />
    public bool Supports(BaseItem item)
    {
        return item is Book;
    }

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        yield return ImageType.Primary;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(
        BaseItem item,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config?.EnableOpenLibrary != true)
        {
            return Enumerable.Empty<RemoteImageInfo>();
        }

        try
        {
            var isbn = item.GetProviderId("ISBN");
            if (string.IsNullOrEmpty(isbn))
            {
                _logger.LogDebug("No ISBN found for {ItemName}", item.Name);
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var images = new List<RemoteImageInfo>();

            // Try to verify if the cover exists by making a HEAD request
            var largeUrl = $"https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg";
            if (await ImageExistsAsync(largeUrl, cancellationToken).ConfigureAwait(false))
            {
                // Add images in priority order: Large → Medium → Small
                images.Add(new RemoteImageInfo
                {
                    Url = largeUrl,
                    Type = ImageType.Primary,
                    ProviderName = Name,
                    Width = 1024,
                    Height = 1365
                });

                images.Add(new RemoteImageInfo
                {
                    Url = $"https://covers.openlibrary.org/b/isbn/{isbn}-M.jpg",
                    Type = ImageType.Primary,
                    ProviderName = Name,
                    Width = 512,
                    Height = 683
                });

                images.Add(new RemoteImageInfo
                {
                    Url = $"https://covers.openlibrary.org/b/isbn/{isbn}-S.jpg",
                    Type = ImageType.Primary,
                    ProviderName = Name,
                    Width = 256,
                    Height = 341
                });

                _logger.LogDebug("Found {Count} images for {ItemName}", images.Count, item.Name);
            }
            else
            {
                _logger.LogDebug("No cover image found for ISBN {ISBN}", isbn);
            }

            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images from Open Library for {ItemName}", item.Name);
            return Enumerable.Empty<RemoteImageInfo>();
        }
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        return httpClient.GetAsync(url, cancellationToken);
    }

    private async Task<bool> ImageExistsAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Open Library returns 404 for missing covers, 200 for existing ones
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking if image exists at {Url}", url);
            return false;
        }
    }
}
