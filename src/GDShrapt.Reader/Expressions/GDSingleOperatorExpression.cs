namespace GDShrapt.Reader
{
    public class GDSingleOperatorExpression : GDExpression
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
                state.PushNode(new GDSingleOperatorResolver((op, comment) =>
                {
                    OperatorType = op;
                    EndLineComment = comment;
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

        /// <summary>
        /// Rebuilds current node if another inner node has higher priority.
        /// </summary>
        /// <returns>Same node if nothing changed or a new node which now the root</returns>
        protected override GDExpression PriorityRebuildingPass()
        {
            if (IsLowerPriorityThan(TargetExpression, GDSideType.Left))
            {
                var previous = TargetExpression;
                TargetExpression = TargetExpression.SwapLeft(this).RebuildOfPriorityIfNeeded();
                return previous;
            }

            return this;
        }


        public override string ToString()
        {
            if (OperatorType == GDSingleOperatorType.Not2)
                return $"{OperatorType.Print()} {TargetExpression}";

            return $"{OperatorType.Print()}{TargetExpression}";
        }
    }
}

