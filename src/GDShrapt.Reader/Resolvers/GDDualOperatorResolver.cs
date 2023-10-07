namespace GDShrapt.Reader
{
    internal class GDDualOperatorResolver : GDPatternResolver
    {
        public new ITokenOrSkipReceiver<GDDualOperator> Owner { get; }
        public GDDualOperatorResolver(ITokenOrSkipReceiver<GDDualOperator> owner)
            : base(owner)
        {
            Owner = owner;
        }

        public override string[] GeneratePatterns()
        {
            return new string[]
            {
                "and",
                "or",
                "as",
                "is",
                "=",
                "<",
                ">",
                "/",
                "*",
                "+",
                "-",
                ">=",
                "<=",
                "==",
                "/=",
                "!=",
                "*=",
                "-=",
                "+=",
                "&&",
                "||",
                "%=",
                "<<",
                ">>",
                "%",
                "^",
                "|",
                "&",
                "in",
                "&=",
                "|=",
                "**",
                "**=",
                 "<<=",
                ">>=",
                "^="
            };
            
        }

        protected override void PatternMatched(string pattern, GDReadingState state)
        {
            switch (pattern)
            {
                case "&&":
                    Return(GDDualOperatorType.And);
                    break;
                case "and":
                    Return(GDDualOperatorType.And2);
                    break;
                case "||":
                    Return(GDDualOperatorType.Or);
                    break;
                case "or":
                    Return(GDDualOperatorType.Or2);
                    break;
                case "as":
                    Return(GDDualOperatorType.As);
                    break;
                case "is":
                    Return(GDDualOperatorType.Is);
                    break;
                case "=":
                    Return(GDDualOperatorType.Assignment);
                    break;
                case "<":
                    Return(GDDualOperatorType.LessThan);
                    break;
                case ">":
                    Return(GDDualOperatorType.MoreThan);
                    break;
                case "/":
                    Return(GDDualOperatorType.Division);
                    break;
                case "*":
                    Return(GDDualOperatorType.Multiply);
                    break;
                case "+":
                    Return(GDDualOperatorType.Addition);
                    break;
                case "-":
                    Return(GDDualOperatorType.Subtraction);
                    break;
                case ">=":
                    Return(GDDualOperatorType.MoreThanOrEqual);
                    break;
                case "<=":
                    Return(GDDualOperatorType.LessThanOrEqual);
                    break;
                case "==":
                    Return(GDDualOperatorType.Equal);
                    break;
                case "/=":
                    Return(GDDualOperatorType.DivideAndAssign);
                    break;
                case "!=":
                    Return(GDDualOperatorType.NotEqual);
                    break;
                case "*=":
                    Return(GDDualOperatorType.MultiplyAndAssign);
                    break;
                case "-=":
                    Return(GDDualOperatorType.SubtractAndAssign);
                    break;
                case "+=":
                    Return(GDDualOperatorType.AddAndAssign);
                    break;
                case "%=":
                    Return(GDDualOperatorType.ModAndAssign);
                    break;
                case "<<":
                    Return(GDDualOperatorType.BitShiftLeft);
                    break;
                case ">>":
                    Return(GDDualOperatorType.BitShiftRight);
                    break;
                case "%":
                    Return(GDDualOperatorType.Mod);
                    break;
                case "^":
                    Return(GDDualOperatorType.Xor);
                    break;
                case "|":
                    Return(GDDualOperatorType.BitwiseOr);
                    break; 
                case "&":
                    Return(GDDualOperatorType.BitwiseAnd);
                    break;
                case "in":
                    Return(GDDualOperatorType.In);
                    break;
                case "&=":
                    Return(GDDualOperatorType.BitwiseAndAndAssign);
                    break;
                case "|=":
                    Return(GDDualOperatorType.BitwiseOrAndAssign);
                    break;
                case "**":
                    Return(GDDualOperatorType.Power);
                    break;
                case "**=":
                    Return(GDDualOperatorType.PowerAndAssign);
                    break;
                case "<<=":
                    Return(GDDualOperatorType.BitShiftLeftAndAssign);
                    break;
                case ">>=":
                    Return(GDDualOperatorType.BitShiftRightAndAssign);
                    break;
                case "^=":
                    Return(GDDualOperatorType.XorAndAssign);
                    break;
                default:
                    Owner.HandleReceivedTokenSkip();

                    if (pattern != null)
                    {
                        for (int i = 0; i < pattern.Length; i++)
                            state.PassChar(pattern[i]);
                    }
                    break;
            }
        }

        void Return(GDDualOperatorType operatorType)
        {
            Owner.HandleReceivedToken(new GDDualOperator() { OperatorType = operatorType });
        }
    }
}
