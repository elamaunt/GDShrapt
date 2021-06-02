using System;

namespace GDShrapt.Reader
{
    internal class GDDualOperatorResolver : GDPattern
    {
        readonly Action<GDDualOperatorType, GDComment> _handler;

        public GDDualOperatorResolver(Action<GDDualOperatorType, GDComment> handler)
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
                    _handler(GDDualOperatorType.And, EndLineComment);
                    break;
                case "and":
                    _handler(GDDualOperatorType.And2, EndLineComment);
                    break;
                case "||":
                    _handler(GDDualOperatorType.Or, EndLineComment);
                    break;
                case "or":
                    _handler(GDDualOperatorType.Or2, EndLineComment);
                    break;
                case "as":
                    _handler(GDDualOperatorType.As, EndLineComment);
                    break;
                case "is":
                    _handler(GDDualOperatorType.Is, EndLineComment);
                    break;
                case "=":
                    _handler(GDDualOperatorType.Assignment, EndLineComment);
                    break;
                case "<":
                    _handler(GDDualOperatorType.LessThan, EndLineComment);
                    break;
                case ">":
                    _handler(GDDualOperatorType.MoreThan, EndLineComment);
                    break;
                case "/":
                    _handler(GDDualOperatorType.Division, EndLineComment);
                    break;
                case "*":
                    _handler(GDDualOperatorType.Multiply, EndLineComment);
                    break;
                case "+":
                    _handler(GDDualOperatorType.Addition, EndLineComment);
                    break;
                case "-":
                    _handler(GDDualOperatorType.Subtraction, EndLineComment);
                    break;
                case ">=":
                    _handler(GDDualOperatorType.MoreThanOrEqual, EndLineComment);
                    break;
                case "<=":
                    _handler(GDDualOperatorType.LessThanOrEqual, EndLineComment);
                    break;
                case "==":
                    _handler(GDDualOperatorType.Equal, EndLineComment);
                    break;
                case "/=":
                    _handler(GDDualOperatorType.DivideAndAssign, EndLineComment);
                    break;
                case "!=":
                    _handler(GDDualOperatorType.NotEqual, EndLineComment);
                    break;
                case "*=":
                    _handler(GDDualOperatorType.MultiplyAndAssign, EndLineComment);
                    break;
                case "-=":
                    _handler(GDDualOperatorType.SubtractAndAssign, EndLineComment);
                    break;
                case "+=":
                    _handler(GDDualOperatorType.AddAndAssign, EndLineComment);
                    break;
                case "%=":
                    _handler(GDDualOperatorType.ModAndAssign, EndLineComment);
                    break;
                case "<<":
                    _handler(GDDualOperatorType.BitShiftLeft, EndLineComment);
                    break;
                case ">>":
                    _handler(GDDualOperatorType.BitShiftRight, EndLineComment);
                    break;
                case "%":
                    _handler(GDDualOperatorType.Mod, EndLineComment);
                    break;
                case "^":
                    _handler(GDDualOperatorType.Xor, EndLineComment);
                    break;
                case "|":
                    _handler(GDDualOperatorType.BitwiseOr, EndLineComment);
                    break; 
                case "&":
                    _handler(GDDualOperatorType.BitwiseAnd, EndLineComment);
                    break;
                case "in":
                    _handler(GDDualOperatorType.In, EndLineComment);
                    break;
                case "&=":
                    _handler(GDDualOperatorType.BitwiseAndAndAssign, EndLineComment);
                    break;
                case "|=":
                    _handler(GDDualOperatorType.BitwiseOrAndAssign, EndLineComment);
                    break;
                default:
                    _handler(GDDualOperatorType.Unknown, EndLineComment);

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
            _handler(GDDualOperatorType.Unknown, EndLineComment);
            state.PopNode();
            state.PassLineFinish();
        }
    }
}
