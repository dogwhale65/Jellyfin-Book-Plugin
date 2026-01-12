using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.BookMetadata.Providers.GoogleBooks;

/// <summary>
/// Google Books API response model.
/// </summary>
public class GoogleBooksResponse
{
    /// <summary>
    /// Gets or sets the kind of resource.
    /// </summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    /// <summary>
    /// Gets or sets the total items count.
    /// </summary>
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    [JsonPropertyName("items")]
    public List<GoogleBooksVolume>? Items { get; set; }
}

/// <summary>
/// Google Books volume model.
/// </summary>
public class GoogleBooksVolume
{
    /// <summary>
    /// Gets or sets the volume ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the volume information.
    /// </summary>
    [JsonPropertyName("volumeInfo")]
    public GoogleBooksVolumeInfo? VolumeInfo { get; set; }
}

/// <summary>
/// Google Books volume information model.
/// </summary>
public class GoogleBooksVolumeInfo
{
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the subtitle.
    /// </summary>
    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets the authors.
    /// </summary>
    [JsonPropertyName("authors")]
    public List<string>? Authors { get; set; }

    /// <summary>
    /// Gets or sets the publisher.
    /// </summary>
    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }

    /// <summary>
    /// Gets or sets the published date.
    /// </summary>
    [JsonPropertyName("publishedDate")]
    public string? PublishedDate { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the industry identifiers.
    /// </summary>
    [JsonPropertyName("industryIdentifiers")]
    public List<GoogleBooksIndustryIdentifier>? IndustryIdentifiers { get; set; }

    /// <summary>
    /// Gets or sets the page count.
    /// </summary>
    [JsonPropertyName("pageCount")]
    public int? PageCount { get; set; }

    /// <summary>
    /// Gets or sets the categories.
    /// </summary>
    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }

    /// <summary>
    /// Gets or sets the average rating.
    /// </summary>
    [JsonPropertyName("averageRating")]
    public double? AverageRating { get; set; }

    /// <summary>
    /// Gets or sets the ratings count.
    /// </summary>
    [JsonPropertyName("ratingsCount")]
    public int? RatingsCount { get; set; }

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the image links.
    /// </summary>
    [JsonPropertyName("imageLinks")]
    public GoogleBooksImageLinks? ImageLinks { get; set; }
}

/// <summary>
/// Google Books industry identifier model.
/// </summary>
public class GoogleBooksIndustryIdentifier
{
    /// <summary>
    /// Gets or sets the identifier type (e.g., "ISBN_10", "ISBN_13").
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the identifier value.
    /// </summary>
    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }
}

/// <summary>
/// Google Books image links model.
/// </summary>
public class GoogleBooksImageLinks
{
    /// <summary>
    /// Gets or sets the small thumbnail URL.
    /// </summary>
    [JsonPropertyName("smallThumbnail")]
    public string? SmallThumbnail { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail URL.
    /// </summary>
    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Gets or sets the small URL.
    /// </summary>
    [JsonPropertyName("small")]
    public string? Small { get; set; }

    /// <summary>
    /// Gets or sets the medium URL.
    /// </summary>
    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    /// <summary>
    /// Gets or sets the large URL.
    /// </summary>
    [JsonPropertyName("large")]
    public string? Large { get; set; }

    /// <summary>
    /// Gets or sets the extra large URL.
    /// </summary>
    [JsonPropertyName("extraLarge")]
    public string? ExtraLarge { get; set; }
}
