# Deployment Guide

## Building the Plugin

### Prerequisites

- .NET 10.0 SDK (or .NET 9.0 SDK - the plugin targets net9.0)
- Git (for version control)

### Build Steps

1. **Restore NuGet packages**:
   ```bash
   cd Jellyfin-Book-Plugin
   dotnet restore
   ```

2. **Build the project**:
   ```bash
   dotnet build Jellyfin.Plugin.BookMetadata/Jellyfin.Plugin.BookMetadata.csproj --configuration Release
   ```

3. **Locate the DLL**:
   The built plugin will be at:
   ```
   Jellyfin.Plugin.BookMetadata/bin/Release/net9.0/Jellyfin.Plugin.BookMetadata.dll
   ```

## Installation Methods

### Method 1: Manual Installation (Direct File Copy)

1. Build the plugin (see above)

2. Copy the DLL and dependencies to Jellyfin's plugin folder:

   **Windows**:
   ```
   %ProgramData%\Jellyfin\Server\plugins\BookMetadata\
   ```

   **Linux**:
   ```
   /var/lib/jellyfin/plugins/BookMetadata/
   ```

   **Docker**:
   ```
   /config/plugins/BookMetadata/
   ```

3. Copy these files:
   - `Jellyfin.Plugin.BookMetadata.dll`
   - `VersOne.Epub.dll`
   - `TagLibSharp.dll`
   - `iTextSharp.LGPLv2.Core.dll`
   - `FuzzySharp.dll`

4. Restart Jellyfin

5. Go to Dashboard → Plugins → Book Metadata to configure

### Method 2: GitHub Release Installation

#### Step 1: Create GitHub Repository

1. Create a new GitHub repository (e.g., `Jellyfin-Book-Plugin`)

2. Push your code:
   ```bash
   cd Jellyfin-Book-Plugin
   git init
   git add .
   git commit -m "Initial commit"
   git remote add origin https://github.com/wmyli/Jellyfin-Book-Plugin.git
   git push -u origin main
   ```

#### Step 2: Create a Release Package

1. Build the plugin in Release mode

2. Create a zip file containing the plugin and dependencies:
   ```bash
   cd Jellyfin.Plugin.BookMetadata/bin/Release/net9.0

   # Windows PowerShell
   Compress-Archive -Path Jellyfin.Plugin.BookMetadata.dll,VersOne.Epub.dll,TagLibSharp.dll,iTextSharp.LGPLv2.Core.dll,FuzzySharp.dll -DestinationPath jellyfin-book-metadata_1.0.0.0.zip

   # Linux/Mac
   zip jellyfin-book-metadata_1.0.0.0.zip Jellyfin.Plugin.BookMetadata.dll VersOne.Epub.dll TagLibSharp.dll iTextSharp.LGPLv2.Core.dll FuzzySharp.dll
   ```

#### Step 3: Create GitHub Release

1. Go to your GitHub repository
2. Click "Releases" → "Create a new release"
3. Tag version: `v1.0.0`
4. Release title: `Book Metadata Plugin v1.0.0`
5. Upload the zip file: `jellyfin-book-metadata_1.0.0.0.zip`
6. Publish release

#### Step 4: Update manifest.json

1. After creating the release, get the checksum:
   ```bash
   # Windows PowerShell
   Get-FileHash jellyfin-book-metadata_1.0.0.0.zip -Algorithm MD5

   # Linux/Mac
   md5sum jellyfin-book-metadata_1.0.0.0.zip
   ```

2. Update `manifest.json` with:
   - The correct `sourceUrl` (GitHub release download URL)
   - The MD5 `checksum`
   - Current `timestamp`

3. Commit and push the updated manifest.json:
   ```bash
   git add manifest.json
   git commit -m "Update manifest with release info"
   git push
   ```

#### Step 5: Add to Jellyfin via Repository URL

1. In Jellyfin, go to Dashboard → Plugins → Repositories

2. Add repository URL:
   ```
   https://raw.githubusercontent.com/wmyli/Jellyfin-Book-Plugin/main/manifest.json
   ```

3. Go to Dashboard → Plugins → Catalog

4. Find "Book Metadata" and click Install

5. Restart Jellyfin when prompted

## Configuration

After installation:

1. Go to Dashboard → Plugins → Book Metadata

2. Configure your preferences:
   - **Metadata Sources**: Enable/disable Google Books and Open Library
   - **Provider Priority**: Set which source to check first (1 = highest)
   - **Rate Limits**: Adjust API request limits if needed
   - **Identification**: Enable ISBN extraction, fuzzy matching, embedded metadata
   - **Fuzzy Match Threshold**: Adjust matching strictness (85 recommended)
   - **Cache Duration**: How long to cache API responses (24 hours recommended)

3. Click Save

## Troubleshooting

### Plugin doesn't appear in Dashboard

- Check that the DLL is in the correct folder
- Ensure all dependency DLLs are present
- Check Jellyfin logs for errors: Dashboard → Logs
- Restart Jellyfin

### No metadata fetched

- Check internet connectivity
- Verify APIs are accessible (Google Books, Open Library)
- Enable debug logging in plugin settings
- Check logs for specific error messages
- Verify books have ISBNs in filenames or file metadata

### Build errors

**If using .NET 10.0 instead of .NET 9.0:**

Update the `.csproj` file:
```xml
<TargetFramework>net10.0</TargetFramework>
```

And update `build.yaml`:
```yaml
framework: "net10.0"
```

Then rebuild:
```bash
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### Wrong metadata matched

- Include ISBN in filename for accurate matching
- Increase fuzzy match threshold (higher = more strict)
- Use folder structure: `Author Name/Book Title/book.epub`

### Missing dependencies error

If you get "Could not load file or assembly" errors, ensure all these DLLs are in the plugin folder:
- `Jellyfin.Plugin.BookMetadata.dll`
- `VersOne.Epub.dll`
- `TagLibSharp.dll`
- `iTextSharp.LGPLv2.Core.dll`
- `FuzzySharp.dll`
- `Microsoft.Extensions.Caching.Memory.dll` (if not in Jellyfin already)

You can find these in the build output folder: `bin/Release/net9.0/`

## Testing

### Test ISBN Extraction
1. Add a book file with ISBN in filename: `The Great Gatsby - 9780743273565.epub`
2. Scan library
3. Check if metadata is fetched correctly

### Test Fuzzy Matching
1. Add a book without ISBN: `The Great Gatsby by F Scott Fitzgerald.epub`
2. Scan library
3. Verify metadata is matched

### Test Embedded Metadata
1. Add an EPUB file with embedded metadata
2. Disable external providers temporarily
3. Scan library
4. Verify embedded metadata is extracted

### Test Cover Images
1. After metadata is fetched
2. Check if cover images are downloaded
3. View book in Jellyfin - cover should be displayed

## Next Steps

1. **Test thoroughly** with your book library
2. **Adjust settings** based on results
3. **Monitor logs** for any issues
4. **Report bugs** at: https://github.com/wmyli/Jellyfin-Book-Plugin/issues

## Updating the Plugin

### Manual Update
1. Build new version
2. Replace DLL in plugins folder
3. Restart Jellyfin

### GitHub Repository Update
1. Create new release with updated version
2. Update `manifest.json` with new version info
3. Jellyfin will show update available in Dashboard → Plugins

## Performance Tips

1. **Use ISBNs in filenames** for fastest, most accurate matching
2. **Enable caching** to reduce API calls during scans
3. **Adjust rate limits** if you have a large library (but respect API terms)
4. **Use embedded metadata** as first choice for books already tagged
5. **Organize files** in folders by author/title for better results

## License Compliance

This plugin uses LGPL-licensed libraries:
- TagLibSharp (LGPL v2.1)
- iTextSharp.LGPLv2.Core (LGPL v2.0)

When distributing, ensure LGPL compliance:
- Source code must be available
- Users must be able to replace LGPL libraries
- Include license notices

## Support

For issues, questions, or contributions:
- GitHub Issues: https://github.com/wmyli/Jellyfin-Book-Plugin/issues
- Jellyfin Forum: https://forum.jellyfin.org/
- Documentation: See README.md
