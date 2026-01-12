using System;
using System.Text.RegularExpressions;
using FuzzySharp;

namespace Jellyfin.Plugin.BookMetadata.Utils;

/// <summary>
/// Utility class for fuzzy matching book titles and authors.
/// </summary>
public partial class FuzzyMatcher
{
    private readonly int _threshold;

    [GeneratedRegex(@"[^\w\s]", RegexOptions.Compiled)]
    private static partial Regex SpecialCharsPattern();

    /// <summary>
    /// Initializes a new instance of the <see cref="FuzzyMatcher"/> class.
    /// </summary>
    /// <param name="threshold">The matching threshold (0-100).</param>
    public FuzzyMatcher(int threshold = 85)
    {
        _threshold = threshold;
    }

    /// <summary>
    /// Determines if two strings match within the threshold.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="target">The target string.</param>
    /// <returns>True if the strings match, false otherwise.</returns>
    public bool IsMatch(string? source, string? target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
        {
            return false;
        }

        // Use token set ratio for better matching with word order changes
        var score = Fuzz.TokenSetRatio(
            NormalizeForMatching(source),
            NormalizeForMatching(target));

        return score >= _threshold;
    }

    /// <summary>
    /// Gets a match score for a search result.
    /// </summary>
    /// <param name="searchTitle">The search title.</param>
    /// <param name="resultTitle">The result title.</param>
    /// <param name="searchYear">The search year.</param>
    /// <param name="resultYear">The result year.</param>
    /// <returns>A match score from 0-100.</returns>
    public int GetMatchScore(
        string? searchTitle,
        string? resultTitle,
        int? searchYear = null,
        int? resultYear = null)
    {
        if (string.IsNullOrEmpty(searchTitle) || string.IsNullOrEmpty(resultTitle))
        {
            return 0;
        }

        var titleScore = Fuzz.TokenSetRatio(
            NormalizeForMatching(searchTitle),
            NormalizeForMatching(resultTitle));

        // If no years provided, return title score only
        if (!searchYear.HasValue || !resultYear.HasValue)
        {
            return titleScore;
        }

        // Calculate year score
        var yearDiff = Math.Abs(searchYear.Value - resultYear.Value);
        var yearScore = yearDiff == 0 ? 100 : Math.Max(0, 100 - (yearDiff * 10));

        // Weighted average: title 70%, year 30%
        return (titleScore * 70 + yearScore * 30) / 100;
    }

    /// <summary>
    /// Gets a match score including author information.
    /// </summary>
    /// <param name="searchTitle">The search title.</param>
    /// <param name="resultTitle">The result title.</param>
    /// <param name="searchAuthor">The search author.</param>
    /// <param name="resultAuthor">The result author.</param>
    /// <param name="searchYear">The search year.</param>
    /// <param name="resultYear">The result year.</param>
    /// <returns>A match score from 0-100.</returns>
    public int GetMatchScore(
        string? searchTitle,
        string? resultTitle,
        string? searchAuthor,
        string? resultAuthor,
        int? searchYear = null,
        int? resultYear = null)
    {
        if (string.IsNullOrEmpty(searchTitle) || string.IsNullOrEmpty(resultTitle))
        {
            return 0;
        }

        var titleScore = Fuzz.TokenSetRatio(
            NormalizeForMatching(searchTitle),
            NormalizeForMatching(resultTitle));

        var authorScore = 0;
        if (!string.IsNullOrEmpty(searchAuthor) && !string.IsNullOrEmpty(resultAuthor))
        {
            authorScore = Fuzz.TokenSetRatio(
                NormalizeForMatching(searchAuthor),
                NormalizeForMatching(resultAuthor));
        }

        var yearScore = 0;
        if (searchYear.HasValue && resultYear.HasValue)
        {
            var yearDiff = Math.Abs(searchYear.Value - resultYear.Value);
            yearScore = yearDiff == 0 ? 100 : Math.Max(0, 100 - (yearDiff * 10));
        }

        // Weighted average based on available data
        if (!string.IsNullOrEmpty(searchAuthor) && searchYear.HasValue)
        {
            // All three: title 50%, author 30%, year 20%
            return (titleScore * 50 + authorScore * 30 + yearScore * 20) / 100;
        }

        if (!string.IsNullOrEmpty(searchAuthor))
        {
            // Title and author: title 60%, author 40%
            return (titleScore * 60 + authorScore * 40) / 100;
        }

        if (searchYear.HasValue)
        {
            // Title and year: title 70%, year 30%
            return (titleScore * 70 + yearScore * 30) / 100;
        }

        // Title only
        return titleScore;
    }

    /// <summary>
    /// Normalizes a string for matching.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The normalized string.</returns>
    private static string NormalizeForMatching(string input)
    {
        // Remove special characters and convert to lowercase
        return SpecialCharsPattern().Replace(input.ToLowerInvariant(), string.Empty);
    }
}
