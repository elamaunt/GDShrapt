namespace GDScriptConverter
{
    public class GDDualOperatorExression : GDExpression
    {
        public GDExpression LeftExpression { get; set; }
        public GDDualOperatorType OperatorType { get; set; }
        public GDExpression RightExpression { get; set; }


        protected internal override void HandleChar(char c, GDReadingState state)
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

        /* private void ReformatIfNeeded(GDDualOperatorExression left, GDDualOperatorExression right)
         {

         }*/

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }

        /* public override GDExpression CombineLeft(GDExpression expr)
         {
             LeftExpression = expr;
             return base.CombineLeft(expr);
         }*/

        public override int Priority
        {
            get
            {
                switch (OperatorType)
                {
                    case GDDualOperatorType.Null:
                    case GDDualOperatorType.Unknown: return 20;
                    case GDDualOperatorType.Division:
                    case GDDualOperatorType.Multiply: return 14;
                    case GDDualOperatorType.Subtraction:
                    case GDDualOperatorType.Addition: return 13;
                    case GDDualOperatorType.Equal:
                    case GDDualOperatorType.NotEqual: return 10;
                    case GDDualOperatorType.Assignment:
                    case GDDualOperatorType.AddAndAssign:
                    case GDDualOperatorType.MultiplyAndAssign:
                    case GDDualOperatorType.DivideAndAssign:
                    case GDDualOperatorType.SubtractAndAssign: return 3;
                    case GDDualOperatorType.MoreThan:
                    case GDDualOperatorType.LessThan:
                    case GDDualOperatorType.LessThanOrEqual:
                    case GDDualOperatorType.MoreThanOrEqual: return 11;
                    case GDDualOperatorType.Or: return 5;
                    case GDDualOperatorType.And: return 6;
                    case GDDualOperatorType.Is:
                    case GDDualOperatorType.As: return 19;
                    default:
                        return 0;
                }
            }
        }

        protected override GDExpression PriorityRebuildingPass()
        {
            var leftPriority = LeftExpression?.Priority;

            if (Priority > leftPriority || (Priority == leftPriority && LeftExpression?.AssociationOrder == GDAssociationOrderType.FromRightToLeft))
            {
                var previous = LeftExpression;
                LeftExpression = LeftExpression.SwapRight(this).RebuildOfPriorityIfNeeded();
                return previous;
            }

            var rightPriority = RightExpression?.Priority;

            if (Priority > rightPriority || (Priority == rightPriority && RightExpression?.AssociationOrder == GDAssociationOrderType.FromLeftToRight))
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