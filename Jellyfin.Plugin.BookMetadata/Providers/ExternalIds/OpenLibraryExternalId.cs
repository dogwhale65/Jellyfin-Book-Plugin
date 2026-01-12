using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.BookMetadata.Providers.ExternalIds;

/// <summary>
/// Open Library external ID provider.
/// </summary>
public class OpenLibraryExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "Open Library";

    /// <inheritdoc />
    public string Key => "OpenLibraryId";

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Book;

    /// <inheritdoc />
    public string UrlFormatString => "https://openlibrary.org{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
    {
        return item is Book;
    }
}
