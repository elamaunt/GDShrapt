using System.Collections;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public abstract class Visitor : IVisitor
    {
        Stack<GDNode> _nodesStack = new Stack<GDNode>();

        public IReadOnlyCollection<GDNode> NodesStack { get; }

        public GDNode Current => _nodesStack.Count > 0 ? _nodesStack.Peek() : default;

        public Visitor()
        {
            NodesStack = new ReadOnlyStack(_nodesStack);
        }

        public virtual void EnterNode(GDNode node)
        {
            _nodesStack.Push(node);
        }

        public virtual void LeftNode()
        {
            _nodesStack.Pop();
        }

        private class ReadOnlyStack : IReadOnlyCollection<GDNode>
        {
            private Stack<GDNode> _nodesStack;

            public ReadOnlyStack(Stack<GDNode> nodesStack)
            {
                _nodesStack = nodesStack;
            }

            public int Count => _nodesStack.Count;

            public IEnumerator<GDNode> GetEnumerator()
            {
                return _nodesStack.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}