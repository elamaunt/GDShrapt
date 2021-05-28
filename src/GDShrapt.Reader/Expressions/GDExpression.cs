﻿using System;

namespace GDShrapt.Reader
{
    public abstract class GDExpression : GDNode
    {
        public abstract int Priority { get; }
        public virtual GDAssociationOrderType AssociationOrder => GDAssociationOrderType.Undefined;

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

        /// <summary>
        /// Swaps expression with left (in terms of position) expression of the parent
        /// </summary>
        /// <param name="expression">Expression to swap</param>
        /// <returns>Expression that was in left position</returns>
        public virtual GDExpression SwapLeft(GDExpression expression)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Swaps expression with right (in terms of position) expression of the parent
        /// </summary>
        /// <param name="expression">Expression to swap</param>
        /// <returns>Expression that was in right position</returns>
        public virtual GDExpression SwapRight(GDExpression expression)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Compares current expression with another one in terms of execution priority
        /// </summary>
        /// <param name="other">Expression</param>
        /// <param name="consideringSide">Relative position of the other expression</param>
        /// <returns>True if current expression have lower priority than other. Otherwise false</returns>
        public bool IsLowerPriorityThan(GDExpression other, GDSideType consideringSide)
        {
            if (other == null)
                return false;

            var thisPriority = Priority;
            var otherPriority = other.Priority;

            if (thisPriority > otherPriority)
                return true;

            if (thisPriority == otherPriority)
            {
                if (consideringSide == GDSideType.Left)
                    return other.AssociationOrder == GDAssociationOrderType.FromRightToLeft;
                else
                    return other.AssociationOrder == GDAssociationOrderType.FromLeftToRight;
            }

            return false;
        }
    }
}