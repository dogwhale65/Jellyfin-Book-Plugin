using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.BookMetadata.Providers.OpenLibrary;

/// <summary>
/// Open Library search response model.
/// </summary>
public class OpenLibrarySearchResponse
{
    /// <summary>
    /// Gets or sets the number of results found.
    /// </summary>
    [JsonPropertyName("numFound")]
    public int NumFound { get; set; }

    /// <summary>
    /// Gets or sets the starting offset.
    /// </summary>
    [JsonPropertyName("start")]
    public int Start { get; set; }

    /// <summary>
    /// Gets or sets the documents.
    /// </summary>
    [JsonPropertyName("docs")]
    public List<OpenLibrarySearchDoc>? Docs { get; set; }
}

/// <summary>
/// Open Library search document model.
/// </summary>
public class OpenLibrarySearchDoc
{
    /// <summary>
    /// Gets or sets the work key.
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

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
    /// Gets or sets the author names.
    /// </summary>
    [JsonPropertyName("author_name")]
    public List<string>? AuthorName { get; set; }

    /// <summary>
    /// Gets or sets the first publish year.
    /// </summary>
    [JsonPropertyName("first_publish_year")]
    public int? FirstPublishYear { get; set; }

    /// <summary>
    /// Gets or sets the ISBNs.
    /// </summary>
    [JsonPropertyName("isbn")]
    public List<string>? Isbn { get; set; }

    /// <summary>
    /// Gets or sets the publisher names.
    /// </summary>
    [JsonPropertyName("publisher")]
    public List<string>? Publisher { get; set; }

    /// <summary>
    /// Gets or sets the languages.
    /// </summary>
    [JsonPropertyName("language")]
    public List<string>? Language { get; set; }

    /// <summary>
    /// Gets or sets the subjects.
    /// </summary>
    [JsonPropertyName("subject")]
    public List<string>? Subject { get; set; }

    /// <summary>
    /// Gets or sets the cover edition key.
    /// </summary>
    [JsonPropertyName("cover_edition_key")]
    public string? CoverEditionKey { get; set; }

    /// <summary>
    /// Gets or sets the cover IDs.
    /// </summary>
    [JsonPropertyName("cover_i")]
    public int? CoverId { get; set; }

    /// <summary>
    /// Gets or sets the edition count.
    /// </summary>
    [JsonPropertyName("edition_count")]
    public int? EditionCount { get; set; }
}

/// <summary>
/// Open Library Books API response model (bibkeys response).
/// </summary>
public class OpenLibraryBooksResponse : Dictionary<string, OpenLibraryBook>
{
}

/// <summary>
/// Open Library book model.
/// </summary>
public class OpenLibraryBook
{
    /// <summary>
    /// Gets or sets the book URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

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
    public List<OpenLibraryAuthor>? Authors { get; set; }

    /// <summary>
    /// Gets or sets the publishers.
    /// </summary>
    [JsonPropertyName("publishers")]
    public List<OpenLibraryPublisher>? Publishers { get; set; }

    /// <summary>
    /// Gets or sets the publish date.
    /// </summary>
    [JsonPropertyName("publish_date")]
    public string? PublishDate { get; set; }

    /// <summary>
    /// Gets or sets the identifiers.
    /// </summary>
    [JsonPropertyName("identifiers")]
    public OpenLibraryIdentifiers? Identifiers { get; set; }

    /// <summary>
    /// Gets or sets the subjects.
    /// </summary>
    [JsonPropertyName("subjects")]
    public List<OpenLibrarySubject>? Subjects { get; set; }

    /// <summary>
    /// Gets or sets the excerpts.
    /// </summary>
    [JsonPropertyName("excerpts")]
    public List<OpenLibraryExcerpt>? Excerpts { get; set; }

    /// <summary>
    /// Gets or sets the cover image.
    /// </summary>
    [JsonPropertyName("cover")]
    public OpenLibraryCover? Cover { get; set; }

    /// <summary>
    /// Gets or sets the number of pages.
    /// </summary>
    [JsonPropertyName("number_of_pages")]
    public int? NumberOfPages { get; set; }
}

/// <summary>
/// Open Library author model.
/// </summary>
public class OpenLibraryAuthor
{
    /// <summary>
    /// Gets or sets the author URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the author name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Open Library publisher model.
/// </summary>
public class OpenLibraryPublisher
{
    /// <summary>
    /// Gets or sets the publisher name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Open Library identifiers model.
/// </summary>
public class OpenLibraryIdentifiers
{
    /// <summary>
    /// Gets or sets the ISBN-10 list.
    /// </summary>
    [JsonPropertyName("isbn_10")]
    public List<string>? Isbn10 { get; set; }

    /// <summary>
    /// Gets or sets the ISBN-13 list.
    /// </summary>
    [JsonPropertyName("isbn_13")]
    public List<string>? Isbn13 { get; set; }

    /// <summary>
    /// Gets or sets the Open Library IDs.
    /// </summary>
    [JsonPropertyName("openlibrary")]
    public List<string>? OpenLibrary { get; set; }

    /// <summary>
    /// Gets or sets the Goodreads IDs.
    /// </summary>
    [JsonPropertyName("goodreads")]
    public List<string>? Goodreads { get; set; }
}

/// <summary>
/// Open Library subject model.
/// </summary>
public class OpenLibrarySubject
{
    /// <summary>
    /// Gets or sets the subject URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the subject name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Open Library excerpt model.
/// </summary>
public class OpenLibraryExcerpt
{
    /// <summary>
    /// Gets or sets the excerpt text.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// Open Library cover model.
/// </summary>
public class OpenLibraryCover
{
    /// <summary>
    /// Gets or sets the small cover URL.
    /// </summary>
    [JsonPropertyName("small")]
    public string? Small { get; set; }

    /// <summary>
    /// Gets or sets the medium cover URL.
    /// </summary>
    [JsonPropertyName("medium")]
    public string? Medium { get; set; }

    /// <summary>
    /// Gets or sets the large cover URL.
    /// </summary>
    [JsonPropertyName("large")]
    public string? Large { get; set; }
}
