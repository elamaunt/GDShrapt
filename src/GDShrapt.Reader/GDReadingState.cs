using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Contains nodes stack and settings during the code reading process.
    /// Manages the reading process.
    /// </summary>
    internal class GDReadingState
    {
        public GDReadSettings Settings { get; }

        GDSimpleSyntaxToken _simpleToken;

        /// <summary>
        /// Main reading stack
        /// </summary>
        readonly Stack<GDNode> _tokensStack = new Stack<GDNode>();
        GDSyntaxToken CurrentToken => (GDSyntaxToken)_simpleToken ?? _tokensStack.PeekOrDefault();

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
            _simpleToken?.ForceComplete(this);

            int count;

            if (_tokensStack.Count == 0)
                return;

            do
            {
                count = _tokensStack.Count;
                CurrentToken.ForceComplete(this);
            }
            while (_tokensStack.Count > 0 && count != _tokensStack.Count);

            if (_tokensStack.Count > 0)
                throw new Exception("Invalid reading state. Nodes stack isn't empty. Last token is: " + CurrentToken);
        }

        public void Push(GDSimpleSyntaxToken token)
        {
            if (_simpleToken != null)
                throw new Exception("Invalid reading state. Current reading token hasn't been droped.");

            _simpleToken = token;
        }

        /// <summary>
        /// Sends new line character '\n' to the current token.
        /// </summary>
        public void PassLineFinish()
        {
            CurrentToken?.HandleLineFinish(this);
        }

        /// <summary>
        /// Sends a character to the current token.
        /// </summary>
        public void PassChar(char c)
        {
            var node = CurrentToken;

            if (node == null)
                return;

            if (c == '#')
            {
                node.HandleSharpChar(this);
                return;
            }

            node.HandleChar(c, this);
        }

        /// <summary>
        /// Adds new node to the stack.
        /// </summary>
        public void Push<T>(T node)
            where T : GDNode
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            _tokensStack.Push(node);
        }

        /// <summary>
        /// Removes last node from the stack.
        /// Usually it is calling by the last node itself.
        /// </summary>
        /// <returns>Removed node</returns>
        public void Pop()
        {
            if (_simpleToken != null)
            {
                _simpleToken = null;
                return;
            }

            _tokensStack.Pop();
        }
    }
}