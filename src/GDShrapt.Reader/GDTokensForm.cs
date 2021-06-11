﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    internal class GDTokensForm<STATE, T0> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
    {
        public GDTokensForm()
            : base(1)
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
        public GDTokensForm()
            : base(2)
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

    internal class GDTokensForm<STATE, T0, T1, T2, T3> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
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

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
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

    internal class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
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
        public GDTokensForm()
            : base(8)
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

    internal abstract class GDTokensForm<STATE> : GDTokensForm
       where STATE : struct, System.Enum
    {
        public GDTokensForm(int size) 
            : base(size)
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
        LinkedList<GDSyntaxToken> _list;
        LinkedListNode<GDSyntaxToken>[] _statePoints;

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public int StateIndex { get; set; }

        public GDTokensForm(int size)
        {
            _list = new LinkedList<GDSyntaxToken>();
            _statePoints = new LinkedListNode<GDSyntaxToken>[size];

            for (int i = 0; i < size; i++)
                _list.AddLast(_statePoints[i] = new LinkedListNode<GDSyntaxToken>(null));
        }

        public void MoveTokens(GDTokensForm baseForm)
        {
            if (baseForm == null)
                return;

            if (StateIndex > 0 || _list.Count > _statePoints.Length)
                throw new InvalidOperationException("MoveTokens supported only for initial form");

            var node = baseForm._list.First;
            var pointNode = baseForm._statePoints[StateIndex];

            while (node != null)
            {
                if (ReferenceEquals(node, pointNode))
                {
                    _statePoints[StateIndex].Value = node.Value;
                    pointNode = baseForm._statePoints[++StateIndex];
                }
                else
                {
                    AddBeforeActiveToken(node.Value);
                }

                node = node.Next;
            }
        }

        public void AddBeforeActiveToken(GDSyntaxToken token)
        {
            AddBeforeToken(token, StateIndex);
        }

        public void AddBeforeToken(GDSyntaxToken token, int index)
        {
            if (token is null)
                throw new System.ArgumentNullException(nameof(token));

            if (index < _statePoints.Length)
                AddMiddle(token, index);
            else
                Add(token);
        }

        public void Add(GDSyntaxToken value)
        {
            if (value is null)
                throw new System.ArgumentNullException(nameof(value));

            _list.AddLast(value);
        }

        protected void AddMiddle(GDSyntaxToken value, int index)
        {
            if (value is null)
                throw new System.ArgumentNullException(nameof(value));

            if (index >= _statePoints.Length)
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
            _statePoints[index].Value = value;
        }

        protected T Get<T>(int index) where T : GDSyntaxToken => (T)_statePoints[index].Value;
        protected GDSyntaxToken Get(int index) => _statePoints[index].Value;

       
        public void Clear()
        {
            var size = _statePoints.Length;
            _list = new LinkedList<GDSyntaxToken>();
            _statePoints = new LinkedListNode<GDSyntaxToken>[size];

            for (int i = 0; i < size; i++)
                _list.AddLast(_statePoints[i] = new LinkedListNode<GDSyntaxToken>(null));
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

            for (int i = 0; i < _statePoints.Length; i++)
            {
                if (_statePoints[i].Value == item)
                {
                    _statePoints[i].Value = null;
                    return true;
                }
            }

            return _list.Remove(item);
        }

        public void Validate()
        {
            // TODO: check all tokens in the linked list on simplicity

            throw new GDInvalidReadingStateException();
        }

        public IEnumerator<GDSyntaxToken> GetEnumerator()
        {
            return _list.OfType<GDSyntaxToken>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}