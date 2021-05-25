namespace GDScriptConverter
{
    public class GDDualOperatorExression : GDExpression
    {
        public GDDualOperator Operator { get; set; }
        public GDExpression LeftExpression { get; set; }
        public GDExpression RightExpression { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if(LeftExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => LeftExpression = expr));
                state.HandleChar(c);
                return;
            }

           /* switch (OperatorType)
            {
                case GDDualOperatorType.Unknown:
                    switch (c)
                    {
                        case '>':
                            OperatorType = GDDualOperatorType.MoreThan;
                            return;
                        case '<':
                            OperatorType = GDDualOperatorType.LessThan;
                            return;
                        case '=':
                            OperatorType = GDDualOperatorType.Assignment;
                            return;
                        case '-':
                            OperatorType = GDDualOperatorType.Subtraction;
                            return;
                        case '+':
                            OperatorType = GDDualOperatorType.Addition;
                            return;
                        case '/':
                            OperatorType = GDDualOperatorType.Division;
                            return;
                        case '*':
                            OperatorType = GDDualOperatorType.Multiply;
                            return;
                        //case '!':
                        //    OperatorType = GDDualOperatorType.Negate;
                        //    return;
                        default:
                            break;
                    }
                    break;
                case GDDualOperatorType.Assignment:
                    if (c == '=')
                    {
                        OperatorType = GDDualOperatorType.Equal;
                        return;
                    }
                    break;
                case GDDualOperatorType.MoreThan:
                    if (c == '=')
                    {
                        OperatorType = GDDualOperatorType.MoreThanOrEqual;
                        return;
                    }
                    break;
                case GDDualOperatorType.LessThan:
                    if (c == '=')
                    {
                        OperatorType = GDDualOperatorType.LessThanOrEqual;
                        return;
                    }
                    break;
                //case GDDualOperatorType.Negate:
                //    if (c == '=')
                //    {
                //        OperatorType = GDDualOperatorType.NotEqual;
                //        return;
                //    }
                //    break;
                case GDDualOperatorType.Multiply:
                    if (c == '=')
                    {
                        OperatorType = GDDualOperatorType.MultiplyAndAssign;
                        return;
                    }
                    break;
                case GDDualOperatorType.Subtraction:
                    if (c == '=')
                    {
                        OperatorType = GDDualOperatorType.SubtractAndAssign;
                        return;
                    }
                    break;
                case GDDualOperatorType.Addition:
                    if (c == '=')
                    {
                        OperatorType = GDDualOperatorType.AddAndAssign;
                        return;
                    }
                    break;
                case GDDualOperatorType.Division:
                    if (c == '=')
                    {
                        OperatorType = GDDualOperatorType.DivideAndAssign;
                        return;
                    }
                    break;
                default:
                    break;
            }*/

            if (RightExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr =>
                {
                    RightExpression = expr;

                    if (expr is GDDualOperatorExression dual)
                        ReformatIfNeeded(this, dual);
                }));
                state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        private void ReformatIfNeeded(GDDualOperatorExression left, GDDualOperatorExression right)
        {

        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.LineFinished();
        }

       /* public override GDExpression CombineLeft(GDExpression expr)
        {
            LeftExpression = expr;
            return base.CombineLeft(expr);
        }*/
    }
}