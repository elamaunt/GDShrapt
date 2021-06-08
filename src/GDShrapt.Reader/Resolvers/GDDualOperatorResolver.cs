using System;

namespace GDShrapt.Reader
{
    internal class GDDualOperatorResolver : GDPattern
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
                "|="
            };
            
        }

        protected override void PatternMatched(string pattern, GDReadingState state)
        {
            switch (pattern)
            {
                case "&&":
                    _handler(GDDualOperatorType.And);
                    break;
                case "and":
                    _handler(GDDualOperatorType.And2);
                    break;
                case "||":
                    _handler(GDDualOperatorType.Or);
                    break;
                case "or":
                    _handler(GDDualOperatorType.Or2);
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
                    _handler(GDDualOperatorType.Addition);
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
                case "%=":
                    _handler(GDDualOperatorType.ModAndAssign);
                    break;
                case "<<":
                    _handler(GDDualOperatorType.BitShiftLeft);
                    break;
                case ">>":
                    _handler(GDDualOperatorType.BitShiftRight);
                    break;
                case "%":
                    _handler(GDDualOperatorType.Mod);
                    break;
                case "^":
                    _handler(GDDualOperatorType.Xor);
                    break;
                case "|":
                    _handler(GDDualOperatorType.BitwiseOr);
                    break; 
                case "&":
                    _handler(GDDualOperatorType.BitwiseAnd);
                    break;
                case "in":
                    _handler(GDDualOperatorType.In);
                    break;
                case "&=":
                    _handler(GDDualOperatorType.BitwiseAndAndAssign);
                    break;
                case "|=":
                    _handler(GDDualOperatorType.BitwiseOrAndAssign);
                    break;
                default:
                    _handler(GDDualOperatorType.Unknown);

                    if (pattern != null)
                    {
                        for (int i = 0; i < pattern.Length; i++)
                            state.PassChar(pattern[i]);
                    }
                    break;
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            _handler(GDDualOperatorType.Unknown);
            state.PopNode();
            state.PassLineFinish();
        }
    }
}
