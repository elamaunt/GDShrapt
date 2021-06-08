namespace GDShrapt.Reader
{
    public sealed class GDSingleOperatorExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperatorPriority(OperatorType);
        public GDSingleOperatorType OperatorType { get; set; }
        public GDExpression TargetExpression { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (OperatorType == GDSingleOperatorType.Null)
            {
                state.PushNode(new GDSingleOperatorResolver((op) =>
                {
                    OperatorType = op;
                }));
                state.PassChar(c);
                return;
            }

            if (TargetExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => TargetExpression = expr));
                state.PassChar(c);
                return;
            }

            state.PopNode();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        protected override GDExpression PriorityRebuildingPass()
        {
            if (IsHigherPriorityThan(TargetExpression, GDSideType.Left))
            {
                var previous = TargetExpression;
                TargetExpression = previous.SwapLeft(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            // Remove 'negate' operator for number expression. Just make the number negative.
            if (OperatorType == GDSingleOperatorType.Negate && TargetExpression is GDNumberExpression numberExpression)
            {
                numberExpression.Number.Negate();
                return numberExpression;
            }

            return this;
        }

        public override GDExpression SwapLeft(GDExpression expression)
        {
            var right = TargetExpression;
            TargetExpression = expression;
            return right;
        }

        public override GDExpression SwapRight(GDExpression expression)
        {
            var right = TargetExpression;
            TargetExpression = expression;
            return right;
        }

        public override void RebuildBranchesOfPriorityIfNeeded()
        {
            TargetExpression = TargetExpression.RebuildRootOfPriorityIfNeeded();
        }

        public override string ToString()
        {
            if (OperatorType == GDSingleOperatorType.Not2)
                return $"{OperatorType.Print()} {TargetExpression}";

            return $"{OperatorType.Print()}{TargetExpression}";
        }
    }
}

