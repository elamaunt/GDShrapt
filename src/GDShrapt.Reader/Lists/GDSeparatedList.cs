using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public abstract class GDSeparatedList<NODE, SEPARATOR> : GDNode, IList<NODE>
        where NODE : GDSyntaxToken
        where SEPARATOR : GDSimpleSyntaxToken, new()
    {
        readonly GDTokensListForm<NODE> _form;
        public GDTokensListForm<NODE> ListForm => _form;
        public override GDTokensForm Form => _form;

        public int Count => _form.Count;
        public bool IsReadOnly => false;

        public GDSeparatedList()
        {
            _form = new GDTokensListForm<NODE>(this);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            Form.Add(state.Push(new GDComment()));
            state.PassSharpChar();
        }

        public new NODE this[int index] 
        {
            get => _form[index];
            set
            {
                _form[index] = value;
            }
        }

        public void Add(NODE item)
        {
            _form.Add(item);
        }

        public void Clear()
        {
            _form.ClearAllTokens();
        }

        public bool Contains(NODE item)
        {
            return _form.Contains(item);
        }

        public void CopyTo(NODE[] array, int arrayIndex)
        {
            _form.CopyTo(array, arrayIndex);
        }

        public bool Remove(NODE item)
        {
            return _form.Remove(item);
        }

        public IEnumerator<NODE> GetEnumerator()
        {
            return ListForm.OfType<NODE>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ListForm.OfType<NODE>().GetEnumerator();
        }

        public int IndexOf(NODE item)
        {
            return _form.IndexOf(item);
        }

        public void Insert(int index, NODE item)
        {
            _form.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _form.RemoveAt(index);
        }
    }
}
