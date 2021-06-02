namespace GDShrapt.Reader
{
    public class GDDualOperatorExression : GDExpression
    {
        bool _leftExpressionChecked;
        bool _rightExpressionChecked;

        public override int Priority => GDHelper.GetOperatorPriority(OperatorType);
        public override GDAssociationOrderType AssociationOrder => GDHelper.GetOperatorAssociationOrder(OperatorType);

        public GDExpression LeftExpression { get; set; }
        public GDDualOperatorType OperatorType { get; set; }
        public GDExpression RightExpression { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (!_leftExpressionChecked && LeftExpression == null)
            {
                _leftExpressionChecked = true;
                state.PushNode(new GDExpressionResolver(expr =>
                {
                    LeftExpression = expr;
                }));
                state.PassChar(c);
                return;
            }

            // Indicates that it isn't a normal expression. The parent should handle the state.
            if (LeftExpression == null)
            {
                state.PopNode();
                state.PassChar(c);
                return;
            }

            if (OperatorType == GDDualOperatorType.Null)
            {
                state.PushNode(new GDDualOperatorResolver((op, comment) =>
                {
                    OperatorType = op;
                    EndLineComment = comment;
                }));
                state.PassChar(c);
                return;
            }

            if (!_rightExpressionChecked && RightExpression == null)
            {
                _rightExpressionChecked = true;

                state.PushNode(new GDExpressionResolver(expr =>
                {
                    RightExpression = expr;
                }));
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