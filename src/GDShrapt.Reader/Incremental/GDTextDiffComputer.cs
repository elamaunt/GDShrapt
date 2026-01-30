using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Computes text changes between two strings.
    /// Used when explicit changes are not available (FileSystemWatcher scenario).
    /// </summary>
    public static class GDTextDiffComputer
    {
        /// <summary>
        /// Computes changes to transform oldText into newText.
        /// Uses line-based diff for efficiency (matches GDScriptIncrementalReader's member-level strategy).
        /// </summary>
        public static IReadOnlyList<GDTextChange> ComputeChanges(string oldText, string newText)
        {
            if (oldText == newText)
                return Array.Empty<GDTextChange>();

            if (string.IsNullOrEmpty(oldText))
                return new[] { GDTextChange.Insert(0, newText) };

            if (string.IsNullOrEmpty(newText))
                return new[] { GDTextChange.Delete(0, oldText.Length) };

            // Line-based diff
            var oldLines = SplitLines(oldText);
            var newLines = SplitLines(newText);

            return ComputeLineDiff(oldText, oldLines, newText, newLines);
        }

        /// <summary>
        /// Fast check if texts differ (without computing full diff).
        /// </summary>
        public static bool TextsDiffer(string oldText, string newText)
            => !string.Equals(oldText, newText, StringComparison.Ordinal);

        private static List<(int Start, int Length)> SplitLines(string text)
        {
            var lines = new List<(int Start, int Length)>();
            int start = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    lines.Add((start, i - start + 1));
                    start = i + 1;
                }
            }

            if (start < text.Length)
                lines.Add((start, text.Length - start));

            return lines;
        }

        private static IReadOnlyList<GDTextChange> ComputeLineDiff(
            string oldText, List<(int Start, int Length)> oldLines,
            string newText, List<(int Start, int Length)> newLines)
        {
            // Simple algorithm: find first and last differing lines
            int firstDiff = 0;
            while (firstDiff < oldLines.Count && firstDiff < newLines.Count)
            {
                var (oldStart, oldLen) = oldLines[firstDiff];
                var (newStart, newLen) = newLines[firstDiff];

                if (!SpansEqual(oldText, oldStart, oldLen, newText, newStart, newLen))
                    break;

                firstDiff++;
            }

            // If texts are identical
            if (firstDiff == oldLines.Count && firstDiff == newLines.Count)
                return Array.Empty<GDTextChange>();

            // Find last differing line (from the end)
            int oldEnd = oldLines.Count - 1;
            int newEnd = newLines.Count - 1;

            while (oldEnd >= firstDiff && newEnd >= firstDiff)
            {
                var (oldStart, oldLen) = oldLines[oldEnd];
                var (newStart, newLen) = newLines[newEnd];

                if (!SpansEqual(oldText, oldStart, oldLen, newText, newStart, newLen))
                    break;

                oldEnd--;
                newEnd--;
            }

            // Calculate change positions
            int changeStartInOld = firstDiff < oldLines.Count ? oldLines[firstDiff].Start : oldText.Length;
            int changeEndInOld = oldEnd >= 0 && oldEnd < oldLines.Count
                ? oldLines[oldEnd].Start + oldLines[oldEnd].Length
                : oldText.Length;

            int changeStartInNew = firstDiff < newLines.Count ? newLines[firstDiff].Start : newText.Length;
            int changeEndInNew = newEnd >= 0 && newEnd < newLines.Count
                ? newLines[newEnd].Start + newLines[newEnd].Length
                : newText.Length;

            var oldLength = changeEndInOld - changeStartInOld;
            var newFragment = newText.Substring(changeStartInNew, changeEndInNew - changeStartInNew);

            return new[] { new GDTextChange(changeStartInOld, oldLength, newFragment) };
        }

        private static bool SpansEqual(string text1, int start1, int len1, string text2, int start2, int len2)
        {
            if (len1 != len2)
                return false;

            for (int i = 0; i < len1; i++)
            {
                if (text1[start1 + i] != text2[start2 + i])
                    return false;
            }

            return true;
        }
    }
}
