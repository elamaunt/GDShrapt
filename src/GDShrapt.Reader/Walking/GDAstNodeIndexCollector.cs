using System;
using System.Collections.Generic;

namespace GDShrapt.Reader
{
    internal sealed class GDAstNodeIndexCollector : GDVisitor
    {
        private readonly HashSet<Type> _filter;

        public List<GDNode> AllNodes { get; } = new List<GDNode>();
        public Dictionary<Type, List<GDNode>> NodesByType { get; } = new Dictionary<Type, List<GDNode>>();

        public GDAstNodeIndexCollector(Type[] filter)
        {
            _filter = filter != null ? new HashSet<Type>(filter) : null;
        }

        public override void WillVisit(GDNode node)
        {
            AllNodes.Add(node);

            var type = node.GetType();

            if (_filter == null || _filter.Contains(type))
            {
                if (!NodesByType.TryGetValue(type, out var list))
                {
                    list = new List<GDNode>();
                    NodesByType[type] = list;
                }

                list.Add(node);
            }
        }
    }
}
