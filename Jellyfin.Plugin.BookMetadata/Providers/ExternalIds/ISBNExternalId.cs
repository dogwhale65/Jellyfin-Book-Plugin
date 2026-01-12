using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.BookMetadata.Providers.ExternalIds;

/// <summary>
/// ISBN external ID provider.
/// </summary>
public class ISBNExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "ISBN";

    /// <inheritdoc />
    public string Key => "ISBN";

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Book;

    /// <inheritdoc />
    public string UrlFormatString => "https://www.worldcat.org/isbn/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
    {
        return item is Book;
    }
}
