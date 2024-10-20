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
        public int? MaxStacktraceFramesCount { get; set; } = 512;
    }
}