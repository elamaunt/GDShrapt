using System;

namespace GDShrapt.Reader
{
    internal class GDSingleOperatorResolver : GDPatternResolver
    {
        public GDSingleOperatorResolver(ITokensContainer owner)
            : base(owner)
        {
        }

        public override string[] GeneratePatterns()
        {
            return new string[]
            {
                "not",
                "-",
                "!",
                "~"
            };
        }

        protected override void PatternMatched(string pattern, GDReadingState state)
        {
            switch (pattern)
            {
                case "!":
                case "not":
                    Append(GDSingleOperatorType.Not);
                    break;
                case "-":
                    Append(GDSingleOperatorType.Negate);
                    break;
                case "~":
                    Append(GDSingleOperatorType.BitwiseNegate);
                    break;
                default:
                    Append(GDSingleOperatorType.Unknown);

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
            Append(GDSingleOperatorType.Unknown);
            state.Pop();
            state.PassLineFinish();
        }

        void Append(GDSingleOperatorType operatorType)
        {
            Append(new GDSingleOperator() { OperatorType = operatorType });
        }
    }
}
