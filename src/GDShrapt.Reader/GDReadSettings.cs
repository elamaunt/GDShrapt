namespace GDShrapt.Reader
{
    public class GDReadSettings
    {
        /// <summary>
        /// Amount of spaces that equals a single tabulation
        /// Default value is 4
        /// </summary>
        public int SingleTabSpacesCost { get; set; } = 4;

        public int ReadBufferSize { get; set; } = 1024;

        /// <summary>
        /// If the reading state exceeds this value a StackOverflowException will be thrown.
        /// Set null and you will gain a real stackoverflow exception with unpredictable behavior.
        /// </summary>
        public int? MaxReadingStack { get; set; } = 64;

        /// <summary>
        /// If the stactrace exceeds this value a StackOverflowException will be thrown.
        /// Set null and you will gain a real stackoverflow exception with unpredictable behavior.
        /// Use it only for debugging
        /// </summary>
        public int? MaxStacktraceFramesCount { get; set; } = null;

        /// <summary>
        /// Interval (in characters) between cancellation token checks during parsing.
        /// Lower values provide faster cancellation response but slightly impact performance.
        /// Higher values improve performance but delay cancellation response.
        /// Set to 0 to disable cancellation checks.
        /// Default value is 256.
        /// </summary>
        public int CancellationCheckInterval { get; set; } = 256;

        /// <summary>
        /// Maximum number of PassChar calls allowed without advancing the input position.
        /// If exceeded, a <see cref="GDInfiniteLoopException"/> is thrown.
        /// This protects against buggy resolvers that re-pass the same character indefinitely.
        /// Set to null to disable loop detection.
        /// Default value is 1000.
        /// </summary>
        public int? MaxPassesWithoutProgress { get; set; } = 1000;

        /// <summary>
        /// Whether to detect possible keyword fragments in invalid tokens
        /// and set PossibleKeyword/StartsWithKeyword metadata on GDInvalidToken.
        /// Default: true
        /// </summary>
        public bool DetectKeywordFragments { get; set; } = true;

        /// <summary>
        /// Minimum fragment length for keyword prefix matching.
        /// Fragments shorter than this are ignored to avoid false positives
        /// on single-letter identifiers (e.g., "v", "f", "i").
        /// Default: 2
        /// </summary>
        public int MinKeywordFragmentLength { get; set; } = 2;
    }
}