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

namespace Jellyfin.Plugin.BookMetadata.Providers.GoogleBooks;

/// <summary>
/// Google Books image provider.
/// </summary>
public class GoogleBooksImageProvider : IRemoteImageProvider
{
    private readonly ILogger<GoogleBooksImageProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleBooksClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleBooksImageProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public GoogleBooksImageProvider(
        ILogger<GoogleBooksImageProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _client = new GoogleBooksClient(httpClientFactory, logger);
    }

    /// <inheritdoc />
    public string Name => "Google Books";

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
        if (config?.EnableGoogleBooks != true)
        {
            return Enumerable.Empty<RemoteImageInfo>();
        }

        try
        {
            var googleBooksId = item.GetProviderId("GoogleBooksId");
            if (string.IsNullOrEmpty(googleBooksId))
            {
                _logger.LogDebug("No Google Books ID found for {ItemName}", item.Name);
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var volume = await _client.GetVolumeAsync(googleBooksId, cancellationToken).ConfigureAwait(false);
            if (volume?.VolumeInfo?.ImageLinks == null)
            {
                _logger.LogDebug("No image links found for Google Books ID {Id}", googleBooksId);
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var imageLinks = volume.VolumeInfo.ImageLinks;
            var images = new List<RemoteImageInfo>();

            // Add images in priority order: ExtraLarge → Large → Medium → Small → Thumbnail
            AddImageIfNotNull(images, imageLinks.ExtraLarge, 1200, 1600);
            AddImageIfNotNull(images, imageLinks.Large, 1024, 1365);
            AddImageIfNotNull(images, imageLinks.Medium, 800, 1067);
            AddImageIfNotNull(images, imageLinks.Small, 600, 800);
            AddImageIfNotNull(images, imageLinks.Thumbnail, 128, 170);
            AddImageIfNotNull(images, imageLinks.SmallThumbnail, 90, 120);

            _logger.LogDebug("Found {Count} images for {ItemName}", images.Count, item.Name);
            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images from Google Books for {ItemName}", item.Name);
            return Enumerable.Empty<RemoteImageInfo>();
        }
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();

        // Ensure HTTPS
        if (url.StartsWith("http:", StringComparison.OrdinalIgnoreCase))
        {
            url = "https:" + url[5..];
        }

        return httpClient.GetAsync(url, cancellationToken);
    }

    private static void AddImageIfNotNull(
        List<RemoteImageInfo> images,
        string? url,
        int? width,
        int? height)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        // Ensure HTTPS
        if (url.StartsWith("http:", StringComparison.OrdinalIgnoreCase))
        {
            url = "https:" + url[5..];
        }

        images.Add(new RemoteImageInfo
        {
            Url = url,
            Type = ImageType.Primary,
            ProviderName = "Google Books",
            Width = width,
            Height = height
        });
    }
}
