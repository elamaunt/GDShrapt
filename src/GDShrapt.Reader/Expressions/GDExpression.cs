using System;

namespace GDShrapt.Reader
{
    public abstract class GDExpression : GDNode
    {
        /// <summary>
        /// Expression priority. Used by expression building algorithm.
        /// </summary>
        public abstract int Priority { get; }

        /// <summary>
        /// Expression AssociationOrder. Used by expression building algorithm, when expressions have equal priority.
        /// </summary>
        public virtual GDAssociationOrderType AssociationOrder => GDAssociationOrderType.Undefined;

        internal GDExpression()
        {

        }

        /// <summary>
        /// Rebuilds expression root node in terms of priority and returns a new root (or the same expression if it is not changed)
        /// </summary>
        /// <returns>Actual root of the current expression tree in terms of priority</returns>

        public GDExpression RebuildRootOfPriorityIfNeeded()
        {
            var root = this;
            bool changed;

            do
            {
                var newRoot = root.PriorityRebuildingPass();
                changed = newRoot != root;

                if (changed)
                    newRoot.RebuildBranchesOfPriorityIfNeeded();
                root = newRoot;
            }
            while (changed);

            return root;
        }

        /// <summary>
        /// Calls <see cref="RebuildRootOfPriorityIfNeeded"/> for inner expressions or do nothing if priority is normal.
        /// </summary>
        public virtual void RebuildBranchesOfPriorityIfNeeded()
        {
            // Nothing
        }

        /// <summary>
        /// Rebuilds current node if another inner node has higher priority.
        /// </summary>
        /// <returns>Same node if nothing changed or a new node which now the root</returns>
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

            if (otherPriority > thisPriority)
                return true;

            if (thisPriority == otherPriority)
            {
                if (consideringSide == GDSideType.Left)
                    return other.AssociationOrder == GDAssociationOrderType.FromLeftToRight;
                else
                    return other.AssociationOrder == GDAssociationOrderType.FromRightToLeft;
            }

            return false;
        }

        /// <summary>
        /// Compares current expression with another one in terms of execution priority
        /// </summary>
        /// <param name="other">Expression</param>
        /// <param name="consideringSide">Relative position of the other expression</param>
        /// <returns>True if current expression have higher priority than other. Otherwise false</returns>
        public bool IsHigherPriorityThan(GDExpression other, GDSideType consideringSide)
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

        public static implicit operator GDExpressionStatement(GDExpression expression)
        {
            return new GDExpressionStatement()
            { 
                Expression = expression
            };
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            state.Pop();
            state.PassSharpChar();
        }
    }
}