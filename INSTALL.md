# Installation Guide

## Build Status: ✅ Complete

The plugin has been built successfully and is ready for deployment.

## Installation Methods

### Method 1: Manual Installation (Recommended for Testing)

1. **Locate your Jellyfin plugins directory:**
   - Windows: `C:\ProgramData\Jellyfin\Server\plugins`
   - Linux: `/var/lib/jellyfin/plugins`
   - Docker: `/config/plugins`

2. **Extract the plugin:**
   - Create a folder named `Jellyfin.Plugin.BookMetadata` in the plugins directory
   - Extract the contents of `Jellyfin.Plugin.BookMetadata_1.0.0.zip` into this folder

3. **Restart Jellyfin:**
   - Restart the Jellyfin server
   - Go to Dashboard → Plugins
   - You should see "Book Metadata" listed

### Method 2: GitHub Installation (Via Raw Link)

1. **Upload the plugin to GitHub:**
   - Upload `Jellyfin.Plugin.BookMetadata_1.0.0.zip` to your GitHub repository
   - Copy the raw URL (e.g., `https://github.com/username/repo/releases/download/v1.0.0/Jellyfin.Plugin.BookMetadata_1.0.0.zip`)

2. **Create a plugin manifest:**
   - Upload the `manifest.json` file to your repository
   - Copy the raw URL for the manifest

3. **Add to Jellyfin:**
   - In Jellyfin Dashboard → Plugins → Repositories
   - Click "+" to add a new repository
   - Enter the manifest URL
   - The plugin should appear in the catalog

## Configuration

After installation:

1. Go to Dashboard → Plugins → Book Metadata
2. Configure your preferences:
   - Enable/disable metadata providers (Google Books, Open Library)
   - Set provider priority
   - Configure rate limits
   - Enable/disable fuzzy matching
   - Set cache duration

## Supported Features

✅ **Metadata Providers:**
- Google Books API (ISBN and title/author search)
- Open Library API (ISBN and title/author search)

✅ **Cover Images:**
- Automatic download from both providers
- Multiple resolution support

✅ **Identification Methods:**
- ISBN extraction from filenames
- Title/Author fuzzy matching
- Provider IDs (GoogleBooksId, ISBN)

✅ **Utilities:**
- In-memory caching (configurable duration)
- Per-provider rate limiting
- Fuzzy string matching for titles

⏸️ **Coming Soon:**
- EPUB embedded metadata extraction
- PDF metadata extraction
- Audiobook tag reading

## File Formats Supported

Currently supports metadata fetching for all book files, identified by:
- ISBN in filename (e.g., `978-0123456789.epub`)
- Title and author in filename (e.g., `Book Title by Author Name.pdf`)

## Testing

1. Add some books to your Jellyfin library with ISBNs in the filename
2. Run a metadata scan
3. Check the logs for any errors: Dashboard → Logs
4. Verify metadata was fetched correctly

## Troubleshooting

**Plugin doesn't appear:**
- Check that files are in the correct directory
- Ensure all DLLs were extracted
- Check Jellyfin logs for errors

**No metadata fetched:**
- Verify providers are enabled in configuration
- Check that rate limits aren't too restrictive
- Look for ISBNs in filenames or use title/author format
- Check API connectivity (Google Books and Open Library must be accessible)

**Warnings in logs:**
- The plugin has some null reference warnings but should function correctly
- These will be addressed in a future update

## Version Info

- **Version:** 1.0.0
- **Target Framework:** .NET 9.0
- **Jellyfin Compatibility:** 10.x
- **Build Date:** 2026-01-12

## Dependencies Included

All required dependencies are included in the zip file:
- FuzzySharp (2.0.2) - Fuzzy string matching
- TagLibSharp (2.3.0) - Audio file tags (for future use)
- VersOne.Epub (3.3.4) - EPUB reading (for future use)
- iTextSharp.LGPLv2.Core (3.4.25) - PDF reading (for future use)
- SkiaSharp (3.116.1) - Image processing
- Various Microsoft.Extensions libraries

## License

This plugin is provided as-is. Third-party libraries included have their own licenses (see individual library documentation).
