using System;

namespace GDShrapt.Abstractions;

/// <summary>
/// Simple glob pattern matcher supporting **, *, and ? wildcards.
/// ** matches any number of path segments (including zero).
/// * matches any characters within a single path segment (does not cross /).
/// ? matches a single character (not /).
/// </summary>
public static class GDGlobMatcher
{
    /// <summary>
    /// Checks if a normalized forward-slash path matches a glob pattern.
    /// </summary>
    public static bool Matches(string path, string pattern)
    {
        if (pattern == "**")
            return true;

        if (pattern.EndsWith("/**"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 3);
            return path.StartsWith(prefix + "/") || path == prefix;
        }

        if (pattern.Contains("**"))
        {
            return DoubleStarMatch(path, pattern);
        }

        if (pattern.Contains("*") || pattern.Contains("?"))
        {
            return SegmentWildcardMatch(path, pattern);
        }

        return path == pattern || path.StartsWith(pattern + "/");
    }

    private static bool DoubleStarMatch(string path, string pattern)
    {
        var parts = pattern.Split(new[] { "**" }, 2, StringSplitOptions.None);
        if (parts.Length != 2)
            return false;

        var prefix = parts[0];
        var suffix = parts[1];

        // prefix/** /suffix — prefix may be empty (for **/ at start)
        if (prefix.Length > 0 && !path.StartsWith(prefix))
            return false;

        if (suffix.Length == 0)
            return true;

        // Remove leading / from suffix if present
        if (suffix.StartsWith("/"))
            suffix = suffix.Substring(1);

        // The remaining path after prefix must contain a segment that matches suffix
        var remaining = prefix.Length > 0 ? path.Substring(prefix.Length) : path;
        if (remaining.StartsWith("/"))
            remaining = remaining.Substring(1);

        // Try matching suffix against each possible tail
        if (suffix.Contains("*") || suffix.Contains("?"))
        {
            // suffix has wildcards — try matching against the filename or various sub-paths
            for (int i = remaining.Length; i >= 0; i--)
            {
                var candidate = i == 0 ? remaining : (i <= remaining.Length ? remaining.Substring(i) : "");
                if (i > 0 && i <= remaining.Length && remaining[i - 1] != '/')
                    continue;
                var sub = i == 0 ? remaining : remaining.Substring(i);
                if (SegmentWildcardMatch(sub, suffix))
                    return true;
            }
            return false;
        }

        // No wildcards in suffix — check containment
        return remaining.Contains("/" + suffix) || remaining == suffix || remaining.EndsWith("/" + suffix.TrimEnd('/'));
    }

    /// <summary>
    /// Matches path against pattern where * does not cross / boundaries.
    /// </summary>
    private static bool SegmentWildcardMatch(string input, string pattern)
    {
        return SegmentMatch(input, 0, pattern, 0);
    }

    private static bool SegmentMatch(string input, int ii, string pattern, int pi)
    {
        while (ii < input.Length && pi < pattern.Length)
        {
            if (pattern[pi] == '?')
            {
                if (input[ii] == '/')
                    return false;
                ii++;
                pi++;
            }
            else if (pattern[pi] == '*')
            {
                pi++;
                // * matches zero or more non-/ characters
                for (int skip = 0; ii + skip <= input.Length; skip++)
                {
                    if (skip > 0 && input[ii + skip - 1] == '/')
                        break;
                    if (SegmentMatch(input, ii + skip, pattern, pi))
                        return true;
                }
                return false;
            }
            else if (pattern[pi] == input[ii])
            {
                ii++;
                pi++;
            }
            else
            {
                return false;
            }
        }

        // Consume trailing *'s in pattern
        while (pi < pattern.Length && pattern[pi] == '*')
            pi++;

        return ii == input.Length && pi == pattern.Length;
    }
}
