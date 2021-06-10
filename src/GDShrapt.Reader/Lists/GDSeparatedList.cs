using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public abstract class GDSeparatedList<NODE, SEPARATOR> : GDNode, IList<NODE>, IStyleTokensReceiver
        where NODE : GDNode
        where SEPARATOR : GDSimpleSyntaxToken, new()
    {
        internal LinkedList<GDSyntaxToken> TokensList { get; } = new LinkedList<GDSyntaxToken>();

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public NODE this[int index] 
        {
            get => TokensList.OfType<NODE>().ElementAt(index);
            set
            {
                var v = TokensList.OfType<NODE>().ElementAt(index);
                var node = TokensList.Find(v);
                node.Value = value;
            }
        }

        public void Add(NODE item)
        {
            if (Count > 0)
                TokensList.AddLast(new SEPARATOR());
            TokensList.AddLast(item);
            Count++;
        }

        public void Clear()
        {
            Count = 0;
            TokensList.Clear();
        }

        public bool Contains(NODE item)
        {
            return this.OfType<NODE>().Contains(item);
        }

        public void CopyTo(NODE[] array, int arrayIndex)
        {
            foreach (var item in TokensList.OfType<NODE>())
                array[arrayIndex++] = item;
        }

        public bool Remove(NODE item)
        {
            if (TokensList.Remove(item))
            {
                Count--;
                return true;
            }

            return false;
        }

        IEnumerator<NODE> IEnumerable<NODE>.GetEnumerator()
        {
            return this.OfType<NODE>().GetEnumerator();
        }

        public int IndexOf(NODE item)
        {
            int index = 0;

            foreach (var token in TokensList.OfType<NODE>())
            {
                if (token == item)
                    return index;
                index++;
            }

            return -1;
        }

        public void Insert(int index, NODE item)
        {
            var v = TokensList.OfType<NODE>().ElementAt(index);
            var node = TokensList.Find(v);

            TokensList.AddBefore(node, new SEPARATOR());
            TokensList.AddBefore(node, item);
            TokensList.AddBefore(node, new SEPARATOR());

            Count++;
        }

        public void RemoveAt(int index)
        {
            if (TokensList.Remove(TokensList.OfType<NODE>().ElementAt(index)))
                Count--;
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDComment token)
        {
            TokensList.AddLast(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            TokensList.AddLast(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDSpace token)
        {
            TokensList.AddLast(token);
        }
        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            TokensList.AddLast(token);
        }
    }
}
