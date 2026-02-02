using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public class GDTokensListForm<TOKEN> : GDTokensForm, IList<TOKEN>
       where TOKEN : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[] { typeof(TOKEN) };
        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            return token is TOKEN;
        }

        internal GDTokensListForm(GDNode owner)
            : base(owner)
        {
        }

        public new int Count => _statePoints.Count;
        public new int TokensCount => base.Count;

        public TOKEN this[int index]
        {
            get => (TOKEN)_statePoints[index].Value;
            set
            {
                ThrowIfFrozen();
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

        public void Add(TOKEN item)
        {
            ThrowIfFrozen();
            item.Parent = _owner;
            _statePoints.Add(_list.AddLast(item));
            StateIndex++;
        }

        public override void AddToEnd(GDSyntaxToken value)
        {
            if (value is TOKEN token)
                Add(token);
            else
                base.AddToEnd(value);
        }

        public override void AddAfterToken(GDSyntaxToken newToken, GDSyntaxToken afterThisToken)
        {
            ThrowIfFrozen();
            if (newToken is TOKEN token)
            {
                if (afterThisToken is TOKEN afterToken)
                {
                    var node = _list.Find(afterToken);

                    if (node == null)
                        throw new NullReferenceException("There is no specific token in the form");

                    var index = _statePoints.IndexOf(node);

                    var nextIndex = index + 1;

                    newToken.Parent = _owner;

                    if (nextIndex == _statePoints.Count)
                        _statePoints.Add(_list.AddAfter(node, token));
                    else
                        _statePoints.Insert(nextIndex, _list.AddAfter(node, token));
                }
                else
                {
                    var node = _list.Find(afterThisToken);

                    if (node == null)
                        throw new NullReferenceException("There is no specific token in the form");

                    LinkedListNode<GDSyntaxToken> nextTypedToken = null;
                    var next = node.Next;
                    while (next != null)
                    {
                        if (next.Value is TOKEN)
                        {
                            nextTypedToken = next;
                            break;
                        }
                        next = next.Next;
                    }

                    newToken.Parent = _owner;

                    if (nextTypedToken == null)
                    {
                        _statePoints.Add(_list.AddAfter(node, token));
                    }
                    else
                    {
                        var index = _statePoints.IndexOf(nextTypedToken);
                        _statePoints.Insert(index, _list.AddAfter(node, token));
                    }
                }
            }
            else
            {
                base.AddAfterToken(newToken, afterThisToken);
            }
        }

        public override void AddBeforeToken(GDSyntaxToken newToken, GDSyntaxToken beforeThisToken)
        {
            ThrowIfFrozen();
            if (newToken is TOKEN token)
            {
                if (beforeThisToken is TOKEN beforeToken)
                {
                    var node = _list.Find(beforeToken);

                    if (node == null)
                        throw new NullReferenceException("There is no specific token in the form");

                    var index = _statePoints.IndexOf(node);

                    var previousIndex = index - 1;
                    newToken.Parent = _owner;

                    _statePoints.Insert(previousIndex, _list.AddBefore(node, token));
                }
                else
                {
                    var node = _list.Find(beforeThisToken);

                    if (node == null)
                        throw new NullReferenceException("There is no specific token in the form");

                    LinkedListNode<GDSyntaxToken> nextTypedToken = null;
                    var next = node.Next;
                    while (next != null)
                    {
                        if (next.Value is TOKEN)
                        {
                            nextTypedToken = next;
                            break;
                        }
                        next = next.Next;
                    }

                    newToken.Parent = _owner;

                    if (nextTypedToken == null)
                    {
                        _statePoints.Add(_list.AddAfter(node, token));
                    }
                    else
                    {
                        var index = _statePoints.IndexOf(nextTypedToken);
                        _statePoints.Insert(index, _list.AddAfter(node, token));
                    }
                }
            }
            else
            {
                base.AddBeforeToken(newToken, beforeThisToken);
            }
        }

        public override void AddBeforeToken(GDSyntaxToken newToken, int statePointIndex)
        {
            ThrowIfFrozen();
            if (newToken is TOKEN token)
            {
                if (statePointIndex < _statePoints.Count)
                {
                    var node = _statePoints[statePointIndex];
                    newToken.Parent = _owner;
                    _statePoints.Insert(statePointIndex, _list.AddBefore(node, token));
                }
                else
                {
                    newToken.Parent = _owner;
                    _statePoints.Add(_list.AddLast(token));
                }
            }
            else
            {
                base.AddBeforeToken(newToken, statePointIndex);
            }
        }

        public bool Contains(TOKEN item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            // Use parent Contains which handles frozen state
            return base.Contains(item);
        }

        public void CopyTo(TOKEN[] array, int arrayIndex)
        {
            for (int i = 0; i < _statePoints.Count; i++)
            {
                var node = _statePoints[i];

                if (node == null || node.Value == null)
                    continue;

                array[arrayIndex++] = (TOKEN)node.Value;
            }
        }

        public int IndexOf(TOKEN item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            if (_isFrozen && _frozenTokenIndex != null)
            {
                // When frozen, we need to find index in typed items only
                // First check if item is in the snapshot at all
                if (!_frozenTokenIndex.ContainsKey(item))
                    return -1;

                // Count typed items before this one
                int typedIndex = 0;
                foreach (var token in Direct())
                {
                    if (token == item)
                        return typedIndex;
                    if (token is TOKEN)
                        typedIndex++;
                }
                return -1;
            }

            var node = _list.Find(item);

            if (node == null)
                return -1;

            return _statePoints.IndexOf(node);
        }

        public void Insert(int index, TOKEN item)
        {
            ThrowIfFrozen();
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            var node = _statePoints[index];
            var newNode = _list.AddBefore(node, item);
            item.Parent = _owner;
            _statePoints.Insert(index, newNode);
        }

        public new void Clear()
        {
            ThrowIfFrozen();
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

        public bool Remove(TOKEN item)
        {
            ThrowIfFrozen();
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            var node = _list.Find(item);

            if (node == null)
                return false;

            item.Parent = null;

            _statePoints.Remove(node);
            _list.Remove(node);
            StateIndex--;
            return true;
        }

        public void RemoveAt(int index)
        {
            ThrowIfFrozen();
            var node = _statePoints[index];

            if (node.Value != null)
                node.Value.Parent = null;

            _statePoints.RemoveAt(index);
            _list.Remove(node);
            StateIndex--;
        }

        IEnumerator<TOKEN> IEnumerable<TOKEN>.GetEnumerator()
        {
            // Use base enumerator when frozen for thread-safety (it uses snapshot)
            if (_isFrozen)
            {
                foreach (var token in Direct())
                {
                    if (token is TOKEN typedToken)
                        yield return typedToken;
                }
                yield break;
            }

            for (int i = 0; i < _statePoints.Count; i++)
                yield return (TOKEN)_statePoints[i].Value;
        }

        new IEnumerator<GDSyntaxToken> GetEnumerator()
        {
            return base.GetEnumerator();
        }
    }
}
