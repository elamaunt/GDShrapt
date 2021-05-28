using System;
using System.Collections.Generic;

namespace GDSharp.Reader
{
    public class GDReadingState
    {
        /// <summary>
        /// Main reading nodes stack
        /// </summary>
        readonly Stack<GDNode> _nodesStack = new Stack<GDNode>();

        public int LineIntendation { get; private set; }
        public bool LineIntendationEnded { get; private set; }

        public GDTypeDeclaration Type { get; internal set; }

        GDNode CurrentNode => _nodesStack.PeekOrDefault();
 
        public GDReadingState()
        {
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

        public void FinishLine()
        {
            CurrentNode?.HandleLineFinish(this);
            LineIntendation = 0;
            LineIntendationEnded = false;
        }

        public void HandleChar(char c)
        {
            var node = CurrentNode;

            if (node == null)
                return;

            if (c == '#')
            {
                node.HandleSharpChar(this);
                return;
            }

            if (!LineIntendationEnded)
            {
                if (c == '\t')
                {
                    LineIntendation++;
                    return;
                }
                else
                {
                    LineIntendationEnded = true;
                }
            }

            node.HandleChar(c, this);
        }

        public T PushNode<T>(T node)
            where T : GDNode
        {
            _nodesStack.Push(node);
            return node;
        }

        public GDNode PopNode()
        {
            return _nodesStack.Pop();
        }
    }
}