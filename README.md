# Jellyfin Book Metadata Plugin

A Jellyfin plugin that automatically fetches and sets metadata for books and audiobooks from Google Books API and Open Library API.

## Features

- **Multiple Metadata Sources**: Google Books API and Open Library API
- **Smart Identification**: ISBN extraction, title/author fuzzy matching, and embedded metadata reading
- **Format Support**:
  - Ebooks: EPUB, PDF, MOBI
  - Audiobooks: M4B, MP3, M4A, FLAC
- **Cover Images**: Automatic download and setting of book cover artwork
- **Metadata Fields**: Title, authors, publication date, publisher, description, language, ISBN, genres/categories, and ratings
- **Performance Features**: In-memory caching, rate limiting, and concurrent API requests

## Project Status

### Completed Components

#### Core Infrastructure

- ✅ Solution and project structure
- ✅ Plugin.cs - Main plugin entry point with singleton instance
- ✅ PluginConfiguration.cs - Complete configuration system with all settings
- ✅ .csproj file with all NuGet dependencies

#### Utility Classes

- ✅ ISBNExtractor - ISBN extraction from filenames with validation (ISBN-10 and ISBN-13)
- ✅ FuzzyMatcher - Title/author fuzzy matching using FuzzySharp with configurable threshold
- ✅ MetadataCache - In-memory caching with size limits and expiration
- ✅ RateLimiter - Token bucket rate limiting for API requests

#### Google Books Integration (Partial)

- ✅ GoogleBooksModels - Complete API response models
- ✅ GoogleBooksClient - HTTP client for API communication with search and fetch methods

### Remaining Implementation

#### Google Books (Remaining)

1. **GoogleBooksProvider.cs** - Implement `IRemoteMetadataProvider<Book, BookInfo>`
   - GetSearchResults method
   - GetMetadata method
   - Use GoogleBooksClient, MetadataCache, RateLimiter, and FuzzyMatcher
   - Map API response to Jellyfin Book metadata

2. **GoogleBooksImageProvider.cs** - Implement `IRemoteImageProvider`
   - GetImages method (return RemoteImageInfo list)
   - GetImageResponse method
   - GetSupportedImages method (return ImageType.Primary)
   - Priority: ExtraLarge → Large → Medium → Small → Thumbnail

3. **GoogleBooksExternalId.cs** - Implement `IExternalId`
   - ProviderName: "Google Books"
   - Key: "GoogleBooksId"
   - Type: ExternalIdMediaType.Book
   - UrlFormatString: "<https://books.google.com/books?id={0}>"

#### Open Library Integration

1. **OpenLibraryModels.cs** - Create API response models
   - BookResponse model for Books API
   - SearchResponse model for Search API
   - Include fields: title, authors, publishers, publish_date, identifiers, subjects, etc.

2. **OpenLibraryClient.cs** - HTTP client
   - SearchByISBN method: `https://openlibrary.org/api/books?bibkeys=ISBN:{isbn}&format=json&jscmd=data`
   - SearchByTitleAuthor method: `https://openlibrary.org/search.json?title={title}&author={author}`
   - GetWork method for fetching work details

3. **OpenLibraryProvider.cs** - Implement `IRemoteMetadataProvider<Book, BookInfo>`
   - Similar structure to GoogleBooksProvider
   - Handle both Books API and Search API responses

4. **OpenLibraryImageProvider.cs** - Implement `IRemoteImageProvider`
   - Cover image URL pattern: `https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg`
   - Sizes: Large (-L), Medium (-M), Small (-S)

5. **OpenLibraryExternalId.cs** - Implement `IExternalId`
   - ProviderName: "Open Library"
   - Key: "OpenLibraryId"
   - Type: ExternalIdMediaType.Book
   - UrlFormatString: "<https://openlibrary.org{0}>"

#### External IDs

1. **ISBNExternalId.cs** - Implement `IExternalId`
   - ProviderName: "ISBN"
   - Key: "ISBN"
   - Type: ExternalIdMediaType.Book
   - UrlFormatString: "<https://www.worldcat.org/isbn/{0}>"

#### Local Metadata Providers

1. **EpubMetadataProvider.cs** - Implement `ILocalMetadataProvider<Book>`
   - Use VersOne.Epub library
   - Extract: title, authors, description, ISBN, publisher, publish date
   - Extract cover image
   - Pattern:

     ```csharp
     using var epubBook = await EpubReader.ReadBookAsync(info.Path);
     var metadata = new Book
     {
         Name = epubBook.Title,
         Overview = epubBook.Description,
         // ... map other fields
     };
     ```

2. **AudiobookMetadataProvider.cs** - Implement `ILocalMetadataProvider<Book>`
   - Use TagLibSharp library
   - Support formats: M4B, MP3, M4A, FLAC
   - Map tags: Title/Album → Name, Comment → Overview, AlbumArtists/Performers → Authors, Composers → Narrator
   - Pattern:

     ```csharp
     using var file = TagLib.File.Create(info.Path);
     var tags = file.Tag;
     var metadata = new Book
     {
         Name = tags.Title ?? tags.Album,
         Overview = tags.Comment,
         // ... map other fields
     };
     ```

3. **PdfMetadataProvider.cs** - Implement `ILocalMetadataProvider<Book>`
   - Use iTextSharp.LGPLv2.Core library
   - Extract from PDF info dictionary: Title, Author, Subject, CreationDate
   - Pattern:

     ```csharp
     using var reader = new PdfReader(info.Path);
     var docInfo = reader.Info;
     var metadata = new Book
     {
         Name = docInfo.GetValueOrDefault("Title"),
         Overview = docInfo.GetValueOrDefault("Subject"),
         // ... map other fields
     };
     ```

#### Configuration UI

1. **configPage.html** - HTML configuration page
   - Metadata Sources section (enable/disable, priority)
   - Identification Methods section (toggles for ISBN, fuzzy matching, embedded metadata)
   - Fuzzy match threshold slider
   - API Rate Limits section (inputs for requests per minute)
   - Caching section (duration input, clear cache button)
   - Advanced section (debug logging toggle, language preference dropdown)
   - JavaScript to save/load settings via Jellyfin plugin API

#### Build Configuration

1. **build.yaml** - Plugin repository build config

   ```yaml
   ---
   version: "1.0.0.0"
   targetAbi: "10.9.0.0"
   overview: "Automatic book and audiobook metadata fetching from Google Books and Open Library"
   owner: "yourusername"
   category: "Metadata"
   guid: "2A8F6C7D-3B9E-4F1A-8D5C-6E2B4A7F9C3D"
   ```

## Implementation Guide

### Provider Implementation Pattern

All metadata providers follow this pattern:

```csharp
public class ExampleProvider : IRemoteMetadataProvider<Book, BookInfo>
{
    private readonly ILogger<ExampleProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MetadataCache _cache;
    private readonly RateLimiter _rateLimiter;
    private readonly FuzzyMatcher _fuzzyMatcher;
    private readonly ExampleClient _client;

    public string Name => "Example Provider";

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(
        BookInfo searchInfo,
        CancellationToken cancellationToken)
    {
        // 1. Check configuration - is provider enabled?
        var config = Plugin.Instance?.Configuration;
        if (config?.EnableExample != true)
            return Enumerable.Empty<RemoteSearchResult>();

        // 2. Check cache
        var cacheKey = MetadataCache.GenerateKey(Name, searchInfo.GetProviderId("ISBN") ?? searchInfo.Name);
        var cached = _cache.Get<List<RemoteSearchResult>>(cacheKey);
        if (cached != null)
            return cached;

        // 3. Apply rate limiting
        await _rateLimiter.WaitAsync(cancellationToken);

        // 4. Search by ISBN if available
        if (!string.IsNullOrEmpty(searchInfo.GetProviderId("ISBN")))
        {
            var results = await SearchByISBN(searchInfo, cancellationToken);
            if (results.Any())
            {
                _cache.Set(cacheKey, results);
                return results;
            }
        }

        // 5. Fall back to title/author search
        var searchResults = await SearchByTitleAuthor(searchInfo, cancellationToken);

        // 6. Filter and sort by match score
        var filtered = searchResults
            .Select(r => new {
                Result = r,
                Score = _fuzzyMatcher.GetMatchScore(
                    searchInfo.Name, r.Name,
                    searchInfo.Year, r.ProductionYear)
            })
            .Where(x => x.Score >= config.FuzzyMatchThreshold)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Result)
            .Take(10)
            .ToList();

        _cache.Set(cacheKey, filtered);
        return filtered;
    }

    public async Task<MetadataResult<Book>> GetMetadata(
        BookInfo info,
        CancellationToken cancellationToken)
    {
        // 1. Get provider ID or search
        // 2. Fetch full metadata
        // 3. Map to Book object
        // 4. Add external IDs
        // 5. Return MetadataResult
    }
}
```

### Testing Checklist

Once implementation is complete:

1. **Build the project**: `dotnet build`
2. **Copy plugin DLL** to Jellyfin plugins folder
3. **Restart Jellyfin** server
4. **Verify plugin appears** in Dashboard → Plugins
5. **Test ISBN matching**: Add book with ISBN in filename
6. **Test title/author matching**: Add book with only title and author
7. **Test EPUB metadata**: Add EPUB file
8. **Test audiobook metadata**: Add M4B or MP3 audiobook
9. **Test cover images**: Verify images are downloaded
10. **Test configuration**: Change settings and verify behavior
11. **Test rate limiting**: Scan large library
12. **Test caching**: Scan same library twice, check logs for cache hits

## Dependencies

All dependencies are specified in the .csproj file:

- Jellyfin.Controller 10.*
- Microsoft.Extensions.Http 10.0.1
- VersOne.Epub 3.3.4
- TagLibSharp 2.3.0
- iTextSharp.LGPLv2.Core 3.4.25
- FuzzySharp 2.0.2
- System.Text.Json 8.0.0

## Building

```bash
dotnet build Jellyfin.Plugin.BookMetadata.sln
```

## Installation

1. Build the plugin
2. Copy `Jellyfin.Plugin.BookMetadata.dll` to Jellyfin's plugins folder:
   - Windows: `%ProgramData%\Jellyfin\Server\plugins\BookMetadata\`
   - Linux: `/var/lib/jellyfin/plugins/BookMetadata/`
   - Docker: `/config/plugins/BookMetadata/`
3. Restart Jellyfin
4. Configure the plugin in Dashboard → Plugins → Book Metadata

## Configuration

### Metadata Sources

- **Google Books**: Priority 1 by default, 10 requests/minute
- **Open Library**: Priority 2 by default, 100 requests/minute

### Identification Methods

- **ISBN Extraction**: Enabled by default
- **Fuzzy Matching**: Enabled by default, threshold 85/100
- **Embedded Metadata**: Enabled by default

### Caching

- Default: 24 hours
- Adjustable in settings

## Troubleshooting

### No metadata fetched

1. Check plugin is enabled in Dashboard
2. Check logs for errors: Dashboard → Logs
3. Enable debug logging in plugin settings
4. Verify internet connectivity to APIs

### Wrong metadata matched

1. Include ISBN in filename for accurate matching
2. Adjust fuzzy match threshold (higher = more strict)
3. Use folder structure: `Author Name/Book Title/book.epub`

### Rate limit errors

1. Reduce scan frequency
2. Increase rate limit settings (if API allows)
3. Check cache is working (should reduce API calls)

## License

This plugin uses LGPL-licensed libraries (TagLibSharp, iTextSharp.LGPLv2.Core). Ensure compliance with LGPL when distributing.

## Contributing

Contributions welcome! Please test thoroughly before submitting PRs.

## Support

Report issues at: <https://github.com/yourusername/Jellyfin-Book-Plugin/issues>
