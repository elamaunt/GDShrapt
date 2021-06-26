using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    internal class GDTokensListForm<NODE> : GDTokensForm, IList<NODE>
        where NODE : GDSyntaxToken
    {
        public GDTokensListForm(GDNode owner)
            : base(owner)
        {
        }

        public new int Count => _statePoints.Count;
        public int TokensCount => base.Count;

        public NODE this[int index]
        {
            get => (NODE)_statePoints[index].Value;
            set
            {
                var node = _statePoints[index];

                if (node.Value == value)
                    return;

                if (node.Value != null)
                    node.Value.Parent = null;

                if (value != null)
                {
                    value.Parent = _owner;
                    node.Value = value;
                }
                else
                    node.Value = null;
            }
        }

        public void Add(NODE item)
        {
            item.Parent = _owner;
            _statePoints.Add(_list.AddLast(item));
            StateIndex++;
        }

        public bool Contains(NODE item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            return _list.Contains(item);
        }

        public void CopyTo(NODE[] array, int arrayIndex)
        {
            for (int i = 0; i < _statePoints.Count; i++)
            {
                var node = _statePoints[i];

                if (node == null || node.Value == null)
                    continue;

                array[arrayIndex++] = (NODE)node.Value;
            }
        }

        public int IndexOf(NODE item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            var node = _list.Find(item);

            if (node == null)
                return -1;

            return _statePoints.IndexOf(node);
        }

        public void Insert(int index, NODE item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            var node = _statePoints[index];
            var newNode =_list.AddBefore(node, item);
            _statePoints.Insert(index, newNode);
        }

        public new void Clear()
        {
            var c = _statePoints.Count;

            for (int i = 0; i < c; i++)
            {
                var node = _statePoints[0];

                if (node.Value != null)
                    node.Value.Parent = null;

                _list.Remove(node);
            }

            _statePoints.Clear();
        }

        public void ClearAllTokens() => base.Clear();

        public bool Remove(NODE item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            var node = _list.Find(item);

            if (node == null)
                return false;

            item.Parent = null;

            _statePoints.Remove(node);
            _list.Remove(node);
            return true;
        }

        public void RemoveAt(int index)
        {
            var node = _statePoints[index];

            if (node.Value != null)
                node.Value.Parent = null;

            _statePoints.RemoveAt(index);
            _list.Remove(node);
        }

        IEnumerator<NODE> IEnumerable<NODE>.GetEnumerator()
        {
            return _statePoints.OfType<NODE>().GetEnumerator();
        }
    }

    internal class GDTokensForm<STATE, T0> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 1)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
    }

    internal class GDTokensForm<STATE, T0, T1> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 2)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => Set(value, 1); }
    }

    internal class GDTokensForm<STATE, T0, T1, T2> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 3)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => Set(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => Set(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => Set(value, 2); }
    }

    internal class GDTokensForm<STATE, T0, T1, T2, T3> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 4)
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

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 5)
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

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 6)
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
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
    }

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 7)
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
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => Set(value, 6); }
    }

    internal class GDTokensForm<STATE, T0,T1,T2,T3,T4,T5,T6,T7> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 8)
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
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => Set(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => Set(value, 7); }
    }

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 9)
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
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => Set(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => Set(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => Set(value, 8); }
    }

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 10)
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
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => Set(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => Set(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => Set(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => Set(value, 9); }
    }

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
        where T10 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 11)
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
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => Set(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => Set(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => Set(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => Set(value, 9); }
        public void AddBeforeToken10(GDSyntaxToken token) => AddMiddle(token, 10);
        public T10 Token10 { get => Get<T10>(10); set => Set(value, 10); }
    }

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
        where T10 : GDSyntaxToken
        where T11 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 12)
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
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => Set(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => Set(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => Set(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => Set(value, 9); }
        public void AddBeforeToken10(GDSyntaxToken token) => AddMiddle(token, 10);
        public T10 Token10 { get => Get<T10>(10); set => Set(value, 10); }
        public void AddBeforeToken11(GDSyntaxToken token) => AddMiddle(token, 11);
        public T11 Token11 { get => Get<T11>(11); set => Set(value, 11); }
    }

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
        where T10 : GDSyntaxToken
        where T11 : GDSyntaxToken
        where T12 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 13)
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
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => Set(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => Set(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => Set(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => Set(value, 9); }
        public void AddBeforeToken10(GDSyntaxToken token) => AddMiddle(token, 10);
        public T10 Token10 { get => Get<T10>(10); set => Set(value, 10); }
        public void AddBeforeToken11(GDSyntaxToken token) => AddMiddle(token, 11);
        public T11 Token11 { get => Get<T11>(11); set => Set(value, 11); }
        public void AddBeforeToken12(GDSyntaxToken token) => AddMiddle(token, 12);
        public T12 Token12 { get => Get<T12>(12); set => Set(value, 12); }
    }

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
        where T10 : GDSyntaxToken
        where T11 : GDSyntaxToken
        where T12 : GDSyntaxToken
        where T13 : GDSyntaxToken
    {
        public GDTokensForm(GDNode owner)
            : base(owner, 14)
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
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => Set(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => Set(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => Set(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => Set(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => Set(value, 9); }
        public void AddBeforeToken10(GDSyntaxToken token) => AddMiddle(token, 10);
        public T10 Token10 { get => Get<T10>(10); set => Set(value, 10); }
        public void AddBeforeToken11(GDSyntaxToken token) => AddMiddle(token, 11);
        public T11 Token11 { get => Get<T11>(11); set => Set(value, 11); }
        public void AddBeforeToken12(GDSyntaxToken token) => AddMiddle(token, 12);
        public T12 Token12 { get => Get<T12>(12); set => Set(value, 12); }
        public void AddBeforeToken13(GDSyntaxToken token) => AddMiddle(token, 13);
        public T13 Token13 { get => Get<T13>(13); set => Set(value, 13); }
    }

    internal abstract class GDTokensForm<STATE> : GDTokensForm
       where STATE : struct, System.Enum
    {
        public GDTokensForm(GDNode owner, int size) 
            : base(owner, size)
        {
        }

        public STATE State
        {
            get => (STATE)(object)StateIndex;
            set => StateIndex = (int)(object)value;
        }
    }

    internal abstract class GDTokensForm : ICollection<GDSyntaxToken>
    {
        protected LinkedList<GDSyntaxToken> _list;
        protected List<LinkedListNode<GDSyntaxToken>> _statePoints;

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public int StateIndex { get; set; }
        public bool IsCompleted => StateIndex == _statePoints.Count;

        protected readonly GDNode _owner;
        readonly int _initialSize;

        public GDTokensForm(GDNode owner, int size)
        {
            _owner = owner;

            _initialSize = size;
            _list = new LinkedList<GDSyntaxToken>();
            _statePoints = new List<LinkedListNode<GDSyntaxToken>>(size);

            for (int i = 0; i < size; i++)
                _statePoints.Add(_list.AddLast(default(GDSyntaxToken)));
        }

        public GDTokensForm(GDNode owner)
        {
            _owner = owner;

            _initialSize = 0;
            _list = new LinkedList<GDSyntaxToken>();
            _statePoints = new List<LinkedListNode<GDSyntaxToken>>();
        }

        public void AddBeforeActiveToken(GDSyntaxToken token)
        {
            AddBeforeToken(token, StateIndex);
        }

        public void AddBeforeToken(GDSyntaxToken token, int index)
        {
            if (token is null)
                throw new System.ArgumentNullException(nameof(token));

            if (index < _statePoints.Count)
                AddMiddle(token, index);
            else
                Add(token);
        }

        public void Add(GDSyntaxToken value)
        {
            if (value is null)
                throw new System.ArgumentNullException(nameof(value));

            value.Parent = _owner;
            _list.AddLast(value);
        }

        protected void AddMiddle(GDSyntaxToken value, int index)
        {
            if (value is null)
                throw new System.ArgumentNullException(nameof(value));

            value.Parent = _owner;

            if (index >= _statePoints.Count)
            {
                _list.AddLast(value);
            }
            else
            {
                var node = _statePoints[index];
                node.List.AddBefore(node, value);
            }
        }

        protected void Set(GDSyntaxToken value, int index)
        {
            var node = _statePoints[index];

            if (value != null)
                value.Parent = _owner;

            node.Value = value;
        }

        /// <summary>
        /// Used only by cloning methods. <see cref="CloneFrom(GDTokensForm)"/>
        /// </summary>
        void SetOrAdd(GDSyntaxToken value, int index)
        {
            if (index >= _statePoints.Count)
            {
                // Only for ListForms
                _statePoints.Add(_list.AddLast(value));

                if (value != null)
                    value.Parent = _owner;
            }
            else
            {
                var node = _statePoints[index];

                if (value != null)
                    value.Parent = _owner;

                node.Value = value;
            }
        }

        protected T Get<T>(int index) where T : GDSyntaxToken => (T)_statePoints[index].Value;
        protected GDSyntaxToken Get(int index) => _statePoints[index].Value;


        public void Clear()
        {
            foreach (var token in _list)
            {
                if (token != null)
                    token.Parent = null;
            }

            _list = new LinkedList<GDSyntaxToken>();
            _statePoints = new List<LinkedListNode<GDSyntaxToken>>(_initialSize);

            if (_initialSize > 0)
                for (int i = 0; i < _initialSize; i++)
                    _statePoints[i] = _list.AddLast((GDSyntaxToken)null);
        }

        public bool Contains(GDSyntaxToken item)
        {
            if (item is null)
                throw new System.ArgumentNullException(nameof(item));

            return _list.Contains(item);
        }

        public void CopyTo(GDSyntaxToken[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(GDSyntaxToken item)
        {
            if (item is null)
                throw new System.ArgumentNullException(nameof(item));

            for (int i = 0; i < _statePoints.Count; i++)
            {
                if (_statePoints[i].Value == item)
                {
                    _statePoints[i].Value = null;
                    item.Parent = null;
                    return true;
                }
            }

            if (_list.Remove(item))
            {
                item.Parent = null;
                return true;
            }

            return false;
        }

        public IEnumerator<GDSyntaxToken> GetEnumerator()
        {
            return _list.Where(x => x != null).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal IEnumerable<GDSyntaxToken> GetAllTokensAfter(int index)
        {
            var node = _statePoints[index];

            var next = node.Next;
            while (next != null)
            {
                if (next.Value != null)
                    yield return next.Value;
                next = next.Next;
            }
        }

        internal int CountTokensBetween(int start, int end)
        {
            var s = _statePoints[start];
            var e = _statePoints[end];

            int counter = 0;
            var next = s.Next;

            while(next != e)
            {
                counter++;
                next = next.Next;
            }

            return counter;
        }

        /// <summary>
        /// Main nodes cloning method. Current form must be empty
        /// </summary>
        /// <param name="form">The form to be cloned</param>
        internal void CloneFrom(GDTokensForm form)
        {
            if ((_initialSize != 0 || form._initialSize != 0) && _initialSize != form._initialSize)
                throw new InvalidOperationException("Forms must have same size or zero");

            if (StateIndex > 0)
                throw new InvalidOperationException("The form must be at initial state");

            if (form._list.Count == 0)
                return;

            var node = form._list.First;
            var point = form._statePoints[StateIndex];

            while (node != null)
            {
                if (point == node)
                {
                    SetOrAdd(node.Value?.Clone(), StateIndex++);
                    point = form._statePoints.ElementAtOrDefault(StateIndex);
                }
                else
                {
                    var clone = node.Value?.Clone();
                    AddBeforeActiveToken(clone);
                }

                node = node.Next;
            }
        }
    }
}
