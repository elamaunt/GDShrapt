using System;
using System.Runtime.Serialization;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Specifies the type of stack overflow that occurred during parsing.
    /// </summary>
    public enum GDStackOverflowType
    {
        /// <summary>
        /// The reading stack (parser state stack) exceeded its maximum depth.
        /// </summary>
        ReadingStack,

        /// <summary>
        /// The call stack trace exceeded its maximum frame count.
        /// </summary>
        StackTraceFrames
    }

    /// <summary>
    /// Exception thrown when the parser exceeds maximum stack depth limits.
    /// This is a controlled exception that prevents actual stack overflow crashes.
    /// </summary>
    public class GDStackOverflowException : Exception
    {
        /// <summary>
        /// The current depth when the exception was thrown.
        /// </summary>
        public int CurrentDepth { get; }

        /// <summary>
        /// The maximum allowed depth that was exceeded.
        /// </summary>
        public int MaxDepth { get; }

        /// <summary>
        /// The type of stack overflow that occurred.
        /// </summary>
        public GDStackOverflowType OverflowType { get; }

        public GDStackOverflowException(int currentDepth, int maxDepth, GDStackOverflowType overflowType)
            : base(FormatMessage(currentDepth, maxDepth, overflowType))
        {
            CurrentDepth = currentDepth;
            MaxDepth = maxDepth;
            OverflowType = overflowType;
        }

        public GDStackOverflowException(int currentDepth, int maxDepth, GDStackOverflowType overflowType, Exception innerException)
            : base(FormatMessage(currentDepth, maxDepth, overflowType), innerException)
        {
            CurrentDepth = currentDepth;
            MaxDepth = maxDepth;
            OverflowType = overflowType;
        }

        protected GDStackOverflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            CurrentDepth = info.GetInt32(nameof(CurrentDepth));
            MaxDepth = info.GetInt32(nameof(MaxDepth));
            OverflowType = (GDStackOverflowType)info.GetInt32(nameof(OverflowType));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(CurrentDepth), CurrentDepth);
            info.AddValue(nameof(MaxDepth), MaxDepth);
            info.AddValue(nameof(OverflowType), (int)OverflowType);
        }

        private static string FormatMessage(int currentDepth, int maxDepth, GDStackOverflowType overflowType)
        {
            switch (overflowType)
            {
                case GDStackOverflowType.ReadingStack:
                    return $"Maximum reading stack depth exceeded. Current: {currentDepth}, Maximum: {maxDepth}. " +
                           "This usually indicates deeply nested code or a parsing loop. " +
                           "You can increase MaxReadingStack in GDReadSettings if needed.";
                case GDStackOverflowType.StackTraceFrames:
                    return $"Maximum stack trace frames exceeded. Current: {currentDepth}, Maximum: {maxDepth}. " +
                           "This usually indicates deeply nested code. " +
                           "You can increase MaxStacktraceFramesCount in GDReadSettings if needed.";
                default:
                    return $"Stack overflow during parsing. Current depth: {currentDepth}, Maximum: {maxDepth}.";
            }
        }
    }
}
