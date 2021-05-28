namespace GDShrapt.Reader
{
    public class GDDualOperatorExression : GDExpression
    {
        public override int Priority => GDHelper.GetOperatorPriority(OperatorType);
        public override GDAssociationOrderType AssociationOrder => GDHelper.GetOperatorAssociationOrder(OperatorType);

        public GDExpression LeftExpression { get; set; }
        public GDDualOperatorType OperatorType { get; set; }
        public GDExpression RightExpression { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (LeftExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr =>
                {
                    LeftExpression = expr;
                }));
                state.HandleChar(c);
                return;
            }

            if (OperatorType == GDDualOperatorType.Null)
            {
                state.PushNode(new GDDualOperatorResolver(op => OperatorType = op));
                state.HandleChar(c);
                return;
            }

            if (RightExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr =>
                {
                    RightExpression = expr;
                }));
                state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }


        /// <summary>
        /// Rebuilds current node if another inner node has higher priority.
        /// </summary>
        /// <returns>Same node if nothing changed or a new node which now the root</returns>
        protected override GDExpression PriorityRebuildingPass()
        {
            if (IsLowerPriorityThan(LeftExpression, GDSideType.Left))
            {
                var previous = LeftExpression;
                LeftExpression = LeftExpression.SwapRight(this).RebuildOfPriorityIfNeeded();
                return previous;
            }

            if (IsLowerPriorityThan(RightExpression, GDSideType.Right))
            {
                var previous = RightExpression;
                RightExpression = RightExpression.SwapLeft(this).RebuildOfPriorityIfNeeded();
                return previous;
            }

            return this;
        }

        public override GDExpression SwapLeft(GDExpression expression)
        {
            var left = LeftExpression;
            LeftExpression = expression;
            return left;
        }

        public override GDExpression SwapRight(GDExpression expression)
        {
            var right = RightExpression;
            RightExpression = expression;
            return right;
        }
        public override string ToString()
        {
            return $"{LeftExpression} {OperatorType.Print()} {RightExpression}";
        }
    }
}