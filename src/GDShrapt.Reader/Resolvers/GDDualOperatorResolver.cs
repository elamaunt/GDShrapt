using System;

namespace GDShrapt.Reader
{
    internal class GDDualOperatorResolver : GDPatternResolver
    {
        public GDDualOperatorResolver(ITokensContainer owner)
            : base(owner)
        {
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
                    Append(GDDualOperatorType.And);
                    break;
                case "and":
                    Append(GDDualOperatorType.And2);
                    break;
                case "||":
                    Append(GDDualOperatorType.Or);
                    break;
                case "or":
                    Append(GDDualOperatorType.Or2);
                    break;
                case "as":
                    Append(GDDualOperatorType.As);
                    break;
                case "is":
                    Append(GDDualOperatorType.Is);
                    break;
                case "=":
                    Append(GDDualOperatorType.Assignment);
                    break;
                case "<":
                    Append(GDDualOperatorType.LessThan);
                    break;
                case ">":
                    Append(GDDualOperatorType.MoreThan);
                    break;
                case "/":
                    Append(GDDualOperatorType.Division);
                    break;
                case "*":
                    Append(GDDualOperatorType.Multiply);
                    break;
                case "+":
                    Append(GDDualOperatorType.Addition);
                    break;
                case "-":
                    Append(GDDualOperatorType.Subtraction);
                    break;
                case ">=":
                    Append(GDDualOperatorType.MoreThanOrEqual);
                    break;
                case "<=":
                    Append(GDDualOperatorType.LessThanOrEqual);
                    break;
                case "==":
                    Append(GDDualOperatorType.Equal);
                    break;
                case "/=":
                    Append(GDDualOperatorType.DivideAndAssign);
                    break;
                case "!=":
                    Append(GDDualOperatorType.NotEqual);
                    break;
                case "*=":
                    Append(GDDualOperatorType.MultiplyAndAssign);
                    break;
                case "-=":
                    Append(GDDualOperatorType.SubtractAndAssign);
                    break;
                case "+=":
                    Append(GDDualOperatorType.AddAndAssign);
                    break;
                case "%=":
                    Append(GDDualOperatorType.ModAndAssign);
                    break;
                case "<<":
                    Append(GDDualOperatorType.BitShiftLeft);
                    break;
                case ">>":
                    Append(GDDualOperatorType.BitShiftRight);
                    break;
                case "%":
                    Append(GDDualOperatorType.Mod);
                    break;
                case "^":
                    Append(GDDualOperatorType.Xor);
                    break;
                case "|":
                    Append(GDDualOperatorType.BitwiseOr);
                    break; 
                case "&":
                    Append(GDDualOperatorType.BitwiseAnd);
                    break;
                case "in":
                    Append(GDDualOperatorType.In);
                    break;
                case "&=":
                    Append(GDDualOperatorType.BitwiseAndAndAssign);
                    break;
                case "|=":
                    Append(GDDualOperatorType.BitwiseOrAndAssign);
                    break;
                default:
                    Append(GDDualOperatorType.Unknown);

                    if (pattern != null)
                    {
                        for (int i = 0; i < pattern.Length; i++)
                            state.PassChar(pattern[i]);
                    }
                    break;
            }
        }

        void Append(GDDualOperatorType operatorType)
        {
            Append(new GDDualOperator() { OperatorType = operatorType });
        }
    }
}
