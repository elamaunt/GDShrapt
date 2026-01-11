using GDShrapt.Reader;

namespace GDShrapt.Formatter
{
    /// <summary>
    /// Result of checking if code is properly formatted.
    /// </summary>
    public class FormatCheckResult
    {
        /// <summary>
        /// True if the code is already properly formatted.
        /// </summary>
        public bool IsFormatted { get; set; }

        /// <summary>
        /// The original input code.
        /// </summary>
        public string OriginalCode { get; set; }

        /// <summary>
        /// The formatted version of the code.
        /// </summary>
        public string FormattedCode { get; set; }

        /// <summary>
        /// Number of lines that differ between original and formatted code.
        /// </summary>
        public int DifferingLineCount { get; set; }

        /// <summary>
        /// Creates a result indicating the code is already formatted.
        /// </summary>
        public static FormatCheckResult AlreadyFormatted(string code)
        {
            return new FormatCheckResult
            {
                IsFormatted = true,
                OriginalCode = code,
                FormattedCode = code,
                DifferingLineCount = 0
            };
        }

        /// <summary>
        /// Creates a result indicating the code needs formatting.
        /// </summary>
        public static FormatCheckResult NeedsFormatting(string original, string formatted)
        {
            return new FormatCheckResult
            {
                IsFormatted = false,
                OriginalCode = original,
                FormattedCode = formatted,
                DifferingLineCount = CountDifferingLines(original, formatted)
            };
        }

        private static int CountDifferingLines(string a, string b)
        {
            if (a == b)
                return 0;

            var linesA = a.Split('\n');
            var linesB = b.Split('\n');

            int count = 0;
            int maxLines = System.Math.Max(linesA.Length, linesB.Length);

            for (int i = 0; i < maxLines; i++)
            {
                string lineA = i < linesA.Length ? linesA[i] : null;
                string lineB = i < linesB.Length ? linesB[i] : null;

                if (lineA != lineB)
                    count++;
            }

            return count;
        }
    }
}
