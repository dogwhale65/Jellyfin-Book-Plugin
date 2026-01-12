using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.BookMetadata.Utils;

/// <summary>
/// Utility class for extracting and validating ISBNs from filenames and metadata.
/// </summary>
public static partial class ISBNExtractor
{
    // ISBN-13 pattern: 978-0-123456-78-9 or 9780123456789
    [GeneratedRegex(@"(?:ISBN[-\s]?13?[-:\s]?)?(?<isbn>97[89][-\s]?\d{1,5}[-\s]?\d{1,7}[-\s]?\d{1,6}[-\s]?\d)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Isbn13Pattern();

    // ISBN-10 pattern: 0-123-45678-9 or 0123456789
    [GeneratedRegex(@"(?:ISBN[-\s]?10?[-:\s]?)?(?<isbn>\d{1,5}[-\s]?\d{1,7}[-\s]?\d{1,6}[-\s]?[\dX])", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Isbn10Pattern();

    [GeneratedRegex(@"[-\s]", RegexOptions.Compiled)]
    private static partial Regex NormalizationPattern();

    /// <summary>
    /// Extracts an ISBN from a filename.
    /// </summary>
    /// <param name="filename">The filename to extract from.</param>
    /// <returns>The extracted ISBN or null if not found.</returns>
    public static string? ExtractFromFilename(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            return null;
        }

        // Try ISBN-13 first
        var match = Isbn13Pattern().Match(filename);
        if (match.Success)
        {
            var isbn = NormalizeISBN(match.Groups["isbn"].Value);
            if (ValidateISBN13(isbn))
            {
                return isbn;
            }
        }

        // Try ISBN-10
        match = Isbn10Pattern().Match(filename);
        if (match.Success)
        {
            var isbn = NormalizeISBN(match.Groups["isbn"].Value);
            if (ValidateISBN10(isbn))
            {
                return isbn;
            }
        }

        return null;
    }

    /// <summary>
    /// Normalizes an ISBN by removing hyphens and spaces.
    /// </summary>
    /// <param name="isbn">The ISBN to normalize.</param>
    /// <returns>The normalized ISBN.</returns>
    public static string NormalizeISBN(string isbn)
    {
        return NormalizationPattern().Replace(isbn, string.Empty).ToUpperInvariant();
    }

    /// <summary>
    /// Validates an ISBN-13 using the checksum algorithm.
    /// </summary>
    /// <param name="isbn">The ISBN-13 to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool ValidateISBN13(string isbn)
    {
        if (string.IsNullOrEmpty(isbn) || isbn.Length != 13)
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            if (!char.IsDigit(isbn[i]))
            {
                return false;
            }

            var digit = isbn[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        var checksum = (10 - (sum % 10)) % 10;
        return isbn[12] - '0' == checksum;
    }

    /// <summary>
    /// Validates an ISBN-10 using the modulo 11 algorithm.
    /// </summary>
    /// <param name="isbn">The ISBN-10 to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool ValidateISBN10(string isbn)
    {
        if (string.IsNullOrEmpty(isbn) || isbn.Length != 10)
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            if (!char.IsDigit(isbn[i]))
            {
                return false;
            }

            sum += (isbn[i] - '0') * (10 - i);
        }

        var lastChar = isbn[9];
        var checksum = lastChar == 'X' ? 10 : lastChar - '0';

        return (sum + checksum) % 11 == 0;
    }

    /// <summary>
    /// Converts an ISBN-10 to ISBN-13.
    /// </summary>
    /// <param name="isbn10">The ISBN-10 to convert.</param>
    /// <returns>The ISBN-13 or null if invalid.</returns>
    public static string? ConvertISBN10ToISBN13(string isbn10)
    {
        if (!ValidateISBN10(isbn10))
        {
            return null;
        }

        var isbn13 = "978" + isbn10[..9];
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = isbn13[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 3;
        }

        var checksum = (10 - (sum % 10)) % 10;
        return isbn13 + checksum;
    }
}
