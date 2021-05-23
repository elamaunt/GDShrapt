namespace GDScriptConverter
{
    public class GDOperatorExression : GDExpression
    {
        public GDOperatorType OperatorType { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            switch (OperatorType)
            {
                case GDOperatorType.Unknown:
                    switch (c)
                    {
                        case '>':
                            OperatorType = GDOperatorType.MoreThan;
                            return;
                        case '<':
                            OperatorType = GDOperatorType.LessThan;
                            return;
                        case '=':
                            OperatorType = GDOperatorType.Assignment;
                            return;
                        case '-':
                            OperatorType = GDOperatorType.Subtraction;
                            return;
                        case '+':
                            OperatorType = GDOperatorType.Addition;
                            return;
                        case '/':
                            OperatorType = GDOperatorType.Division;
                            return;
                        case '*':
                            OperatorType = GDOperatorType.Multiply;
                            return;
                        case '!':
                            OperatorType = GDOperatorType.Negate;
                            return;
                        default:
                            break;
                    }
                    break;
                case GDOperatorType.Assignment:
                    if (c == '=')
                    {
                        OperatorType = GDOperatorType.Equal;
                        return;
                    }
                    break;
                case GDOperatorType.MoreThan:
                    if (c == '=')
                    {
                        OperatorType = GDOperatorType.MoreThanOrEqual;
                        return;
                    }
                    break;
                case GDOperatorType.LessThan:
                    if (c == '=')
                    {
                        OperatorType = GDOperatorType.LessThanOrEqual;
                        return;
                    }
                    break;
                case GDOperatorType.Negate:
                    if (c == '=')
                    {
                        OperatorType = GDOperatorType.NotEqual;
                        return;
                    }
                    break;
                case GDOperatorType.Multiply:
                    if (c == '=')
                    {
                        OperatorType = GDOperatorType.MultiplyAndAssign;
                        return;
                    }
                    break;
                case GDOperatorType.Subtraction:
                    if (c == '=')
                    {
                        OperatorType = GDOperatorType.SubtractAndAssign;
                        return;
                    }
                    break;
                case GDOperatorType.Addition:
                    if (c == '=')
                    {
                        OperatorType = GDOperatorType.AddAndAssign;
                        return;
                    }
                    break;
                case GDOperatorType.Division:
                    if (c == '=')
                    {
                        OperatorType = GDOperatorType.DivideAndAssign;
                        return;
                    }
                    break;
                default:
                    break;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.LineFinished();
        }
    }
}