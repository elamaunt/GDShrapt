using System;
using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDReadingState
    {
        /// <summary>
        /// Main reading nodes stack
        /// </summary>
        readonly Stack<GDNode> _nodesStack = new Stack<GDNode>();

        /// <summary>
        /// Additional stack to manage expressions hierarchies properly
        /// </summary>
        readonly Stack<GDExpressionResolver> _expressionResolversStack = new Stack<GDExpressionResolver>(); 

        public int LineIntendation { get; private set; }
        public bool LineIntendationEnded { get; private set; }

        public GDTypeDeclaration Type { get; internal set; }

        GDNode CurrentNode => _nodesStack.PeekOrDefault();
        GDNode CurrentExpressionsResolver => _expressionResolversStack.PeekOrDefault();

        public GDReadingState()
        {
        }

        public void LineStarted()
        {
            LineIntendation = 0;
            LineIntendationEnded = false;
        }

        public void CompleteReading()
        {
            int count;

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

            if (node is GDExpressionResolver resolver)
                _expressionResolversStack.Push(resolver);

            return node;
        }

        public GDNode PopNode()
        {
            var node = _nodesStack.Pop();

            if (node == CurrentExpressionsResolver)
                _expressionResolversStack.Pop();

            return node;
        }
    }
}