using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public class GDAstNodeIndex
    {
        private readonly Dictionary<Type, List<GDNode>> _nodesByType;
        private readonly List<GDNode> _allNodes;

        private GDAstNodeIndex(Dictionary<Type, List<GDNode>> nodesByType, List<GDNode> allNodes)
        {
            _nodesByType = nodesByType;
            _allNodes = allNodes;
        }

        public IReadOnlyList<GDNode> AllNodes => _allNodes;

        public int TotalCount => _allNodes.Count;

        public IReadOnlyList<T> GetNodes<T>() where T : GDNode
        {
            if (_nodesByType.TryGetValue(typeof(T), out var list))
                return new CastList<T>(list);
            return Array.Empty<T>();
        }

        public int Count<T>() where T : GDNode
        {
            if (_nodesByType.TryGetValue(typeof(T), out var list))
                return list.Count;
            return 0;
        }

        public bool HasAny<T>() where T : GDNode
        {
            return _nodesByType.TryGetValue(typeof(T), out var list) && list.Count > 0;
        }

        public static GDAstNodeIndex Build(GDNode root)
        {
            var collector = new GDAstNodeIndexCollector(filter: null);
            root.WalkIn(collector);
            return new GDAstNodeIndex(collector.NodesByType, collector.AllNodes);
        }

        public static GDAstNodeIndex Build(GDNode root, params Type[] typesToIndex)
        {
            var collector = new GDAstNodeIndexCollector(typesToIndex);
            root.WalkIn(collector);
            return new GDAstNodeIndex(collector.NodesByType, collector.AllNodes);
        }

        private sealed class CastList<T> : IReadOnlyList<T> where T : GDNode
        {
            private readonly List<GDNode> _inner;

            public CastList(List<GDNode> inner)
            {
                _inner = inner;
            }

            public T this[int index] => (T)_inner[index];
            public int Count => _inner.Count;

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var node in _inner)
                    yield return (T)node;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
