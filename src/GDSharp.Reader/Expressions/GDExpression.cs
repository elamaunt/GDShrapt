using System;

namespace GDSharp.Reader
{
    public abstract class GDExpression : GDNode
    {
        public abstract int Priority { get; }
        public virtual GDAssociationOrderType AssociationOrder => GDAssociationOrderType.FromLeftToRight;

        /// <summary>
        /// Rebuilds expression tree from current node in terms of priority and returns a new root (or the same expression if it is not changed)
        /// </summary>
        /// <returns>Actual root of the current expression tree in terms of priority</returns>

        public GDExpression RebuildOfPriorityIfNeeded()
        {
            var root = this;
            bool changed;

            do
            {
                var newRoot = root.PriorityRebuildingPass();
                changed = newRoot != root;
                root = newRoot;
            }
            while (changed);

            return root;
        }

        protected virtual GDExpression PriorityRebuildingPass()
        {
            return this;
        }

        public virtual GDExpression SwapLeft(GDExpression expression)
        {
            throw new InvalidOperationException();
        }
        public virtual GDExpression SwapRight(GDExpression expression)
        {
            throw new InvalidOperationException();
        }
    }
}