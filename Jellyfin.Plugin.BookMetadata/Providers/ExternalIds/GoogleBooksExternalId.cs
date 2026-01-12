using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.BookMetadata.Providers.ExternalIds;

/// <summary>
/// Google Books external ID provider.
/// </summary>
public class GoogleBooksExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "Google Books";

    /// <inheritdoc />
    public string Key => "GoogleBooksId";

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Book;

    /// <inheritdoc />
    public string UrlFormatString => "https://books.google.com/books?id={0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
    {
        return item is Book;
    }
}
