using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.BookMetadata.Configuration;

/// <summary>
/// Plugin configuration for Book Metadata.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the Google Books priority (1 = highest).
    /// </summary>
    public int GoogleBooksPriority { get; set; } = 1;

    /// <summary>
    /// Gets or sets the Open Library priority (1 = highest).
    /// </summary>
    public int OpenLibraryPriority { get; set; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether Google Books is enabled.
    /// </summary>
    public bool EnableGoogleBooks { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Open Library is enabled.
    /// </summary>
    public bool EnableOpenLibrary { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether ISBN extraction from filenames is enabled.
    /// </summary>
    public bool EnableISBNExtraction { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether fuzzy matching is enabled.
    /// </summary>
    public bool EnableFuzzyMatching { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether embedded metadata reading is enabled.
    /// </summary>
    public bool EnableEmbeddedMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the Google Books rate limit (requests per minute).
    /// </summary>
    public int GoogleBooksRateLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets the Open Library rate limit (requests per minute).
    /// </summary>
    public int OpenLibraryRateLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the cache duration in hours.
    /// </summary>
    public int CacheDuration { get; set; } = 24;

    /// <summary>
    /// Gets or sets the fuzzy match threshold (0-100).
    /// </summary>
    public int FuzzyMatchThreshold { get; set; } = 85;

    /// <summary>
    /// Gets or sets a value indicating whether debug logging is enabled.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the preferred language code (e.g., "en", "es", "fr").
    /// </summary>
    public string PreferredLanguage { get; set; } = "en";
}
