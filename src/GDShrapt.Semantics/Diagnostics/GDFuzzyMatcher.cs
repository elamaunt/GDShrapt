using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Provides fuzzy string matching for typo detection and suggestions.
/// Uses Levenshtein distance algorithm.
/// </summary>
public static class GDFuzzyMatcher
{
    /// <summary>
    /// Maximum edit distance to consider as a similar match.
    /// </summary>
    private const int MaxDistance = 3;

    /// <summary>
    /// Finds similar strings from a list of candidates.
    /// </summary>
    /// <param name="name">The name to find matches for.</param>
    /// <param name="candidates">The list of candidate names to search.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>Similar names ordered by similarity (most similar first).</returns>
    public static IEnumerable<string> FindSimilar(string name, IEnumerable<string> candidates, int maxResults = 3)
    {
        if (string.IsNullOrEmpty(name))
            yield break;

        var nameLower = name.ToLowerInvariant();

        var matches = candidates
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => (Name: c, Distance: LevenshteinDistance(nameLower, c.ToLowerInvariant())))
            .Where(x => x.Distance > 0 && x.Distance <= MaxDistance)
            .OrderBy(x => x.Distance)
            .ThenBy(x => Math.Abs(x.Name.Length - name.Length))
            .Select(x => x.Name)
            .Distinct()
            .Take(maxResults);

        foreach (var match in matches)
            yield return match;
    }

    /// <summary>
    /// Checks if two strings are similar within the maximum distance threshold.
    /// </summary>
    /// <param name="s1">First string.</param>
    /// <param name="s2">Second string.</param>
    /// <returns>True if similar, false otherwise.</returns>
    public static bool AreSimilar(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return false;

        var distance = LevenshteinDistance(s1.ToLowerInvariant(), s2.ToLowerInvariant());
        return distance > 0 && distance <= MaxDistance;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// </summary>
    /// <param name="s1">First string.</param>
    /// <param name="s2">Second string.</param>
    /// <returns>The edit distance between the strings.</returns>
    public static int LevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;

        // Quick exit for empty strings
        if (n == 0) return m;
        if (m == 0) return n;

        var d = new int[n + 1, m + 1];

        // Initialize first column
        for (var i = 0; i <= n; i++)
            d[i, 0] = i;

        // Initialize first row
        for (var j = 0; j <= m; j++)
            d[0, j] = j;

        // Fill the matrix
        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
