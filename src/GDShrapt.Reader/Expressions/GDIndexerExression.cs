namespace GDShrapt.Reader
{
    public sealed class GDIndexerExression : GDExpression
    {
        private bool _openSquareBracketChecked;

        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Indexer);

        public GDExpression CallerExpression { get; set; }
        public GDExpression InnerExpression { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (CallerExpression == null)
            {
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }

            if (!_openSquareBracketChecked)
            {
                if (c != '[')
                    return;

                _openSquareBracketChecked = true;
                return;
            }

            if (InnerExpression == null)
            {
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        protected override GDExpression PriorityRebuildingPass()
        {
            if (IsHigherPriorityThan(CallerExpression, GDSideType.Left))
            {
                var previous = CallerExpression;
                CallerExpression = previous.SwapRight(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            return this;
        }

        public override GDExpression SwapLeft(GDExpression expression)
        {
            var left = CallerExpression;
            CallerExpression = expression;
            return left;
        }

        public override GDExpression SwapRight(GDExpression expression)
        {
            var right = CallerExpression;
            CallerExpression = expression;
            return right;
        }

        public override void RebuildBranchesOfPriorityIfNeeded()
        {
            CallerExpression = CallerExpression.RebuildRootOfPriorityIfNeeded();
        }

        public override string ToString()
        {
            return $"{CallerExpression}[{InnerExpression}]";
        }
    }
}
