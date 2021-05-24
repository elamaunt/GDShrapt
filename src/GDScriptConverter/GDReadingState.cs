using System;
using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDReadingState
    {
        readonly Stack<GDNode> _nodesStack = new Stack<GDNode>();

        public int LineIntendation;
        public bool LineIntendationEnded;

        public GDTypeDeclaration Type { get; internal set; }

        GDNode CurrentNode => _nodesStack.Peek();

        public GDReadingState()
        {
        }

        public void LineStarted()
        {
            LineIntendation = 0;
            LineIntendationEnded = false;
        }

        public void ContentStarted()
        {
            PushNode(new GDTypeDeclarationResolver(this));
        }

        public void ContentFinished()
        {
            var count = _nodesStack.Count;

            do
            {
                count = _nodesStack.Count;
                CurrentNode.ForceComplete(this);
            }
            while (_nodesStack.Count > 0 && count != _nodesStack.Count);

            if (_nodesStack.Count > 0)
                throw new Exception("Invalid reading state. Nodes stack isn't empty. Last node is: " + CurrentNode);
        }

        public void LineFinished()
        {
            CurrentNode.HandleLineFinish(this);
        }

        public void HandleChar(char c)
        {
            if (c == '#')
            {
                CurrentNode.HandleSharpChar(this);
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

            CurrentNode.HandleChar(c, this);
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