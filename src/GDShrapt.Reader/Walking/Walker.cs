using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public abstract class Walker
    {
        public abstract void WalkInNodes(IEnumerable<GDNode> nodes);
    }
}