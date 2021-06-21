using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Contains readers stack and settings during the code reading process.
    /// Manages the reading process.
    /// </summary>
    internal class GDReadingState
    {
        public GDReadSettings Settings { get; }

        /// <summary>
        /// Main reading stack
        /// </summary>
        readonly Stack<GDReader> _readersStack = new Stack<GDReader>();
        GDReader CurrentReader => _readersStack.PeekOrDefault();

        public GDReadingState(GDReadSettings settings)
        {
            Settings = settings;
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
                throw new GDInvalidReadingStateException("Invalid reading state. Readers stack isn't empty. Last reader is: " + CurrentReader);
        }

        /// <summary>
        /// Sends new line character '\n' to the current reader.
        /// </summary>
        public void PassNewLine()
        {
            CurrentReader.HandleNewLineChar(this);
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
            CurrentReader.HandleSharpChar(this);
        }

        /// <summary>
        /// Sends a character to the current reader.
        /// </summary>
        public void PassChar(char c)
        {
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