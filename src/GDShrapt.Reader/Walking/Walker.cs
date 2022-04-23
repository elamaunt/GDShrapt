using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public abstract class Walker
    {
        public bool WalkBackward { get; set; }
        public abstract void WalkInNodes(IEnumerable<GDNode> nodes);
    }
}