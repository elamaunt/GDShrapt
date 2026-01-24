using System;
using System.Runtime.Serialization;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Exception thrown when the parser detects an infinite loop.
    /// This occurs when PassChar is called repeatedly without advancing the input position.
    /// </summary>
    public class GDInfiniteLoopException : Exception
    {
        /// <summary>
        /// The number of PassChar calls without progress when the exception was thrown.
        /// </summary>
        public int PassCount { get; }

        /// <summary>
        /// The maximum allowed passes without progress.
        /// </summary>
        public int MaxPasses { get; }

        /// <summary>
        /// The character that was being repeatedly passed.
        /// </summary>
        public char? LastChar { get; }

        /// <summary>
        /// The type name of the current reader when the loop was detected.
        /// </summary>
        public string ReaderTypeName { get; }

        public GDInfiniteLoopException(int passCount, int maxPasses, char? lastChar, string readerTypeName)
            : base(FormatMessage(passCount, maxPasses, lastChar, readerTypeName))
        {
            PassCount = passCount;
            MaxPasses = maxPasses;
            LastChar = lastChar;
            ReaderTypeName = readerTypeName;
        }

        public GDInfiniteLoopException(int passCount, int maxPasses, char? lastChar, string readerTypeName, Exception innerException)
            : base(FormatMessage(passCount, maxPasses, lastChar, readerTypeName), innerException)
        {
            PassCount = passCount;
            MaxPasses = maxPasses;
            LastChar = lastChar;
            ReaderTypeName = readerTypeName;
        }

        protected GDInfiniteLoopException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            PassCount = info.GetInt32(nameof(PassCount));
            MaxPasses = info.GetInt32(nameof(MaxPasses));
            LastChar = (char?)info.GetValue(nameof(LastChar), typeof(char?));
            ReaderTypeName = info.GetString(nameof(ReaderTypeName));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(PassCount), PassCount);
            info.AddValue(nameof(MaxPasses), MaxPasses);
            info.AddValue(nameof(LastChar), LastChar);
            info.AddValue(nameof(ReaderTypeName), ReaderTypeName);
        }

        private static string FormatMessage(int passCount, int maxPasses, char? lastChar, string readerTypeName)
        {
            var charInfo = lastChar.HasValue
                ? $"'{lastChar.Value}' (0x{(int)lastChar.Value:X4})"
                : "unknown";

            return $"Infinite loop detected in parser. " +
                   $"PassChar called {passCount} times without advancing input (max: {maxPasses}). " +
                   $"Last character: {charInfo}. " +
                   $"Current reader: {readerTypeName ?? "unknown"}. " +
                   "This indicates a bug in a resolver that keeps re-passing the same character. " +
                   "You can increase MaxPassesWithoutProgress in GDReadSettings if this is expected behavior.";
        }
    }
}
