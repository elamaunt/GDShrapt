using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    internal class GDReadingState
    {
        public GDReadSettings Settings { get; }

        /// <summary>
        /// Main reading nodes stack
        /// </summary>
        readonly Stack<GDNode> _nodesStack = new Stack<GDNode>();
        GDNode CurrentNode => _nodesStack.PeekOrDefault();


        public GDReadingState(GDReadSettings settings)
        {
            Settings = settings;
        }

        public void CompleteReading()
        {
            int count;

            if (_nodesStack.Count == 0)
                return;

            do
            {
                count = _nodesStack.Count;
                CurrentNode.ForceComplete(this);
            }
            while (_nodesStack.Count > 0 && count != _nodesStack.Count);

            if (_nodesStack.Count > 0)
                throw new Exception("Invalid reading state. Nodes stack isn't empty. Last node is: " + CurrentNode);
        }

        public void PassLineFinish()
        {
            CurrentNode?.HandleLineFinish(this);
        }

        public void PassChar(char c)
        {
            var node = CurrentNode;

            if (node == null)
                return;

            if (c == '#')
            {
                node.HandleSharpChar(this);
                return;
            }

            node.HandleChar(c, this);
        }

        public T PushNode<T>(T node)
            where T : GDNode
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            _nodesStack.Push(node);
            return node;
        }

        public GDNode PopNode()
        {
            return _nodesStack.Pop();
        }
    }
}