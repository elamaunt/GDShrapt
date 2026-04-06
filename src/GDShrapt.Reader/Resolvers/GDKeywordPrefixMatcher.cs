using System;

namespace GDShrapt.Reader
{
    internal static class GDKeywordPrefixMatcher
    {
        internal static readonly string[] ClassLevelKeywords =
            { "var", "const", "func", "class", "signal", "enum", "static", "extends", "class_name", "tool", "pass" };

        internal static readonly string[] StatementLevelKeywords =
            { "if", "for", "while", "match", "var", "return", "break", "continue", "pass", "elif", "else", "await", "assert" };

        /// <summary>
        /// Returns the shortest keyword that starts with the given fragment.
        /// E.g., "va" → "var", "fu" → "func".
        /// Returns null if fragment is too short or no match found.
        /// </summary>
        internal static string FindPrefixMatch(string fragment, string[] keywords, int minLength)
        {
            if (fragment == null || fragment.Length < minLength)
                return null;

            string best = null;

            for (int i = 0; i < keywords.Length; i++)
            {
                var kw = keywords[i];

                if (kw.Length > fragment.Length &&
                    kw.StartsWith(fragment, StringComparison.Ordinal) &&
                    (best == null || kw.Length < best.Length))
                {
                    best = kw;
                }
            }

            return best;
        }

        /// <summary>
        /// If the sequence starts with a keyword followed by identifier chars,
        /// returns that keyword. E.g., "varx" → "var", "functest" → "func".
        /// Prefers the longest keyword match.
        /// Returns null if no match found.
        /// </summary>
        internal static string FindKeywordStartMatch(string sequence, string[] keywords)
        {
            if (sequence == null || sequence.Length < 3)
                return null;

            string best = null;

            for (int i = 0; i < keywords.Length; i++)
            {
                var kw = keywords[i];

                if (sequence.Length > kw.Length &&
                    sequence.StartsWith(kw, StringComparison.Ordinal) &&
                    IsIdentifierStartChar(sequence[kw.Length]) &&
                    (best == null || kw.Length > best.Length))
                {
                    best = kw;
                }
            }

            return best;
        }

        /// <summary>
        /// Checks if concatenation exactly matches a keyword.
        /// </summary>
        internal static string FindExactMatch(string concatenated, string[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                if (keywords[i] == concatenated)
                    return keywords[i];
            }

            return null;
        }

        private static bool IsIdentifierStartChar(char c)
        {
            return c == '_' || char.IsLetter(c);
        }
    }
}
