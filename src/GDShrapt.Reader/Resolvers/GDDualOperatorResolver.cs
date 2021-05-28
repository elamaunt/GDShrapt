using System;

namespace GDShrapt.Reader
{
    public class GDDualOperatorResolver : GDPattern
    {
        readonly Action<GDDualOperatorType> _handler;

        public GDDualOperatorResolver(Action<GDDualOperatorType> handler)
        {
            _handler = handler;
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
                "||"
            };
            
        }

        protected override void PatternMatched(string pattern)
        {
            switch (pattern)
            {
                case "&&":
                case "and":
                    _handler(GDDualOperatorType.And);
                    break;
                case "||":
                case "or":
                    _handler(GDDualOperatorType.Or);
                    break;
                case "as":
                    _handler(GDDualOperatorType.As);
                    break;
                case "is":
                    _handler(GDDualOperatorType.Is);
                    break;
                case "=":
                    _handler(GDDualOperatorType.Assignment);
                    break;
                case "<":
                    _handler(GDDualOperatorType.LessThan);
                    break;
                case ">":
                    _handler(GDDualOperatorType.MoreThan);
                    break;
                case "/":
                    _handler(GDDualOperatorType.Division);
                    break;
                case "*":
                    _handler(GDDualOperatorType.Multiply);
                    break;
                case "+":
                    _handler(GDDualOperatorType.AddAndAssign);
                    break;
                case "-":
                    _handler(GDDualOperatorType.Subtraction);
                    break;
                case ">=":
                    _handler(GDDualOperatorType.MoreThanOrEqual);
                    break;
                case "<=":
                    _handler(GDDualOperatorType.LessThanOrEqual);
                    break;
                case "==":
                    _handler(GDDualOperatorType.Equal);
                    break;
                case "/=":
                    _handler(GDDualOperatorType.DivideAndAssign);
                    break;
                case "!=":
                    _handler(GDDualOperatorType.NotEqual);
                    break;
                case "*=":
                    _handler(GDDualOperatorType.MultiplyAndAssign);
                    break;
                case "-=":
                    _handler(GDDualOperatorType.SubtractAndAssign);
                    break;
                case "+=":
                    _handler(GDDualOperatorType.AddAndAssign);
                    break;
                default:
                    _handler(GDDualOperatorType.Unknown);
                    break;
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            _handler(GDDualOperatorType.Unknown);
            state.PopNode();
            state.FinishLine();
        }
    }
}
