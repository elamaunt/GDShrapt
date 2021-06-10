using System.Collections;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    internal class GDTokensForm<T0> : GDTokensForm
        where T0 : GDSyntaxToken
    {
        public GDTokensForm()
            : base(1)
        {

        }

        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
    }

    internal class GDTokensForm<T0, T1> : GDTokensForm
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
    {
        public GDTokensForm()
            : base(2)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => Set(value, 1); }
    }

    internal class GDTokensForm<T0, T1, T2> : GDTokensForm
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
    {
        public GDTokensForm()
            : base(3)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => Set(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => Set(value, 2); }
    }

    internal class GDTokensForm<T0, T1, T2, T3> : GDTokensForm
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
    {
        public GDTokensForm()
            : base(4)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => Set(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => Set(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => Set(value, 3); }
    }

    internal class GDTokensForm<T0, T1, T2, T3, T4> : GDTokensForm
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
    {
        public GDTokensForm()
            : base(5)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => Set(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => Set(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => Set(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => Set(value, 4); }


    }

    internal class GDTokensForm<T0, T1, T2, T3, T4, T5> : GDTokensForm
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
    {
        public GDTokensForm()
            : base(6)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 0);
        public T1 Token1 { get => Get<T1>(1); set => Set(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 1);
        public T2 Token2 { get => Get<T2>(2); set => Set(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 2);
        public T3 Token3 { get => Get<T3>(3); set => Set(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 3);
        public T4 Token4 { get => Get<T4>(4); set => Set(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 4);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
    }

    internal class GDTokensForm<T0, T1, T2, T3, T4, T5, T6> : GDTokensForm
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
    {
        public GDTokensForm()
            : base(7)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 0);
        public T1 Token1 { get => Get<T1>(1); set => Set(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 1);
        public T2 Token2 { get => Get<T2>(2); set => Set(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 2);
        public T3 Token3 { get => Get<T3>(3); set => Set(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 3);
        public T4 Token4 { get => Get<T4>(4); set => Set(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 4);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 5);
        public T6 Token6 { get => Get<T6>(6); set => Set(value, 6); }
    }

    internal class GDTokensForm<T0,T1,T2,T3,T4,T5,T6,T7> : GDTokensForm
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
    {
        public GDTokensForm()
            : base(8)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 0);
        public T1 Token1 { get => Get<T1>(1); set => Set(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 1);
        public T2 Token2 { get => Get<T2>(2); set => Set(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 2);
        public T3 Token3 { get => Get<T3>(3); set => Set(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 3);
        public T4 Token4 { get => Get<T4>(4); set => Set(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 4);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 5);
        public T6 Token6 { get => Get<T6>(6); set => Set(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 6);
        public T7 Token7 { get => Get<T7>(7); set => Set(value, 7); }
    }

    internal abstract class GDTokensForm : IEnumerable<GDSyntaxToken>
    {
        LinkedList<GDSyntaxToken> _list;
        LinkedListNode<GDSyntaxToken>[] _array;

        public GDTokensForm(int size)
        {
            _list = new LinkedList<GDSyntaxToken>();
            _array = new LinkedListNode<GDSyntaxToken>[size];

            for (int i = 0; i < size; i++)
                _list.AddLast(_array[i] = new LinkedListNode<GDSyntaxToken>(null));

        }

        public void AddBeforeToken(GDSyntaxToken token, int index)
        {
            if (index < _array.Length)
                AddMiddle(token, index);
            else
                AddLast(token);
        }

        public void AddLast(GDSyntaxToken value)
        {
            _list.AddLast(value);
        }

        protected void AddMiddle(GDSyntaxToken value, int index)
        {
            if (index >= _array.Length)
            {
                _list.AddLast(value);
            }
            else
            {
                var node = _array[index];
                node.List.AddBefore(node, value);
            }
        }

        protected void Set(GDSyntaxToken value, int index)
        {
            _array[index].Value = value;
        }

        protected T Get<T>(int index) where T : GDSyntaxToken => (T)_array[index].Value;
        protected GDSyntaxToken Get(int index) => _array[index].Value;

        public IEnumerator<GDSyntaxToken> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
