using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Contains readers stack and settings during the code reading process.
    /// Manages the reading process.
    /// </summary>
    internal class GDReadingState
    {
        /// <summary>
        /// Counter for characters processed since last cancellation check.
        /// </summary>
        private int _charsSinceLastCheck;

        /// <summary>
        /// Counter for PassChar calls since last AdvanceInputPosition.
        /// Reset to 0 when input advances. Throws if exceeds MaxPassesWithoutProgress.
        /// </summary>
        private int _passCountSinceAdvance;

        public GDReadSettings Settings { get; }

        /// <summary>
        /// Cancellation token for aborting parsing.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Main reading stack
        /// </summary>
        readonly Stack<GDReader> _readersStack = new Stack<GDReader>(64);
        GDReader CurrentReader => _readersStack.PeekOrDefault();

        /// <summary>
        /// The first intendation in the code is used for the next ones as a pattern like in the Godot's editor.
        /// </summary>
        public int? IntendationInSpacesCount { get; set; } = null;

        #region Pending Chars Buffering

        /// <summary>
        /// Pending characters (spaces, split tokens) that haven't found a home yet.
        /// Used when a node completes and trailing whitespace should go to parent.
        /// </summary>
        private StringBuilder _pendingChars;

        /// <summary>
        /// Set to true when repassing chars. Nodes should not re-buffer during repass.
        /// </summary>
        internal bool IsRepassingChars { get; private set; }

        /// <summary>
        /// Check if there are pending characters.
        /// </summary>
        internal bool HasPendingChars => _pendingChars != null && _pendingChars.Length > 0;

        /// <summary>
        /// Add a character to the pending buffer.
        /// </summary>
        internal void AddPendingChar(char c)
        {
            if (_pendingChars == null)
                _pendingChars = new StringBuilder();

            _pendingChars.Append(c);
        }

        /// <summary>
        /// Add a string to the pending buffer.
        /// </summary>
        internal void AddPendingString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return;

            if (_pendingChars == null)
                _pendingChars = new StringBuilder();

            _pendingChars.Append(s);
        }

        /// <summary>
        /// Re-pass pending characters to the current reader.
        /// Used when a node completes and whitespace should go to parent.
        /// </summary>
        internal void RepassPendingChars()
        {
            if (_pendingChars == null || _pendingChars.Length == 0)
                return;

            var length = _pendingChars.Length;

            // Copy to buffer since PassChar may modify _pendingChars
            // For typical case (1-2 spaces) reuse static buffer to avoid allocations
            char[] buffer;
            if (length <= _repassBuffer.Length)
            {
                buffer = _repassBuffer;
            }
            else
            {
                buffer = new char[length];
            }

            for (int i = 0; i < length; i++)
                buffer[i] = _pendingChars[i];

            _pendingChars.Clear();

            IsRepassingChars = true;
            try
            {
                for (int i = 0; i < length; i++)
                    PassChar(buffer[i]);
            }
            finally
            {
                IsRepassingChars = false;
            }
        }

        readonly char[] _repassBuffer = new char[16];

        /// <summary>
        /// If the current reader is a GDCharSequence (like GDSpace), completes it and pops from stack.
        /// Used after repass to finalize any char sequence tokens that were created during repass.
        /// </summary>
        internal void CompleteActiveCharSequence()
        {
            if (CurrentReader is GDCharSequence charSeq)
            {
                charSeq.CompleteSequence(this);
            }
        }

        #endregion

        public GDReadingState(GDReadSettings settings)
            : this(settings, CancellationToken.None)
        {
        }

        public GDReadingState(GDReadSettings settings, CancellationToken cancellationToken)
        {
            Settings = settings;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Force completes the reading process. 
        /// Usually is called when the code has ended.
        /// </summary>
        public void CompleteReading()
        {
            int count;

            if (_readersStack.Count == 0)
                return;

            do
            {
                count = _readersStack.Count;
                CurrentReader.ForceComplete(this);
            }
            while (_readersStack.Count > 0 && count != _readersStack.Count);

            if (_readersStack.Count > 0)
                throw new GDInvalidStateException("Invalid reading state. Readers stack isn't empty. Last reader is: " + CurrentReader);
        }

        /// <summary>
        /// Sends new line character '\n' to the current reader.
        /// </summary>
        public void PassNewLine()
        {
            CurrentReader?.HandleNewLineChar(this);
        }

        /// <summary>
        /// Sends all characters from the string to the current reader.
        /// </summary>
        public void PassString(string s)
        {
            for (int i = 0; i < s.Length; i++)
                PassChar(s[i]);
        }

        public void PassSharpChar()
        {
            CurrentReader?.HandleSharpChar(this);
        }

        public void PassLeftSlashChar()
        {
            CurrentReader?.HandleLeftSlashChar(this);
        }

        /// <summary>
        /// Signals that the parser has advanced to a new character from the input stream.
        /// This resets the loop detection counter.
        /// Called before PassChar for each new character from the external input.
        /// </summary>
        public void AdvanceInputPosition()
        {
            _passCountSinceAdvance = 0;
        }

        /// <summary>
        /// Sends a character to the current reader.
        /// </summary>
        public void PassChar(char c)
        {
            // Check cancellation periodically based on settings (0 = disabled)
            var interval = Settings.CancellationCheckInterval;
            if (interval > 0 && ++_charsSinceLastCheck >= interval)
            {
                _charsSinceLastCheck = 0;
                CancellationToken.ThrowIfCancellationRequested();
            }

            // Loop detection: count PassChar calls since last input advance
            var maxPasses = Settings.MaxPassesWithoutProgress;
            if (maxPasses.HasValue)
            {
                _passCountSinceAdvance++;
                if (_passCountSinceAdvance > maxPasses.Value)
                {
                    var readerTypeName = CurrentReader?.GetType().Name;
                    throw new GDInfiniteLoopException(_passCountSinceAdvance, maxPasses.Value, c, readerTypeName);
                }
            }

            var reader = CurrentReader;

            if (reader == null)
                return;

            if (c == '\r')
                return;

            if (c == '\n')
            {
                reader.HandleNewLineChar(this);
                return;
            }

            if (c == '#')
            {
                reader.HandleSharpChar(this);
                return;
            }

            if (c == '\\')
            {
                reader.HandleLeftSlashChar(this);
                return;
            }

            reader.HandleChar(c, this);
        }

        /// <summary>
        /// Adds new reader to the stack.
        /// </summary>
        public T Push<T>(T reader)
            where T : GDReader
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            if (Settings.MaxReadingStack.HasValue && _readersStack.Count >= Settings.MaxReadingStack.Value)
                throw new GDStackOverflowException(_readersStack.Count, Settings.MaxReadingStack.Value, GDStackOverflowType.ReadingStack);

            if (Settings.MaxStacktraceFramesCount.HasValue)
            {
                var frameCount = new StackTrace(false).FrameCount;
                if (frameCount >= Settings.MaxStacktraceFramesCount.Value)
                    throw new GDStackOverflowException(frameCount, Settings.MaxStacktraceFramesCount.Value, GDStackOverflowType.StackTraceFrames);
            }

            _readersStack.Push(reader);
            return reader;
        }

        /// <summary>
        /// Removes last reader from the stack.
        /// Usually it is calling by the last reader itself.
        /// </summary>
        public void Pop()
        {
            _readersStack.Pop();
        }

        public void PopAndPass(char c)
        {
            _readersStack.Pop();
            PassChar(c);
        }

        public T PushAndPass<T>(T reader, char c)
            where T : GDReader
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            _readersStack.Push(reader);
            PassChar(c);
            return reader;
        }

        public T PushAndPassNewLine<T>(T reader)
           where T : GDReader
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            _readersStack.Push(reader);
            PassNewLine();
            return reader;
        }

        public void PopAndPassNewLine()
        {
            _readersStack.Pop();
            PassNewLine();
        }
    }
}