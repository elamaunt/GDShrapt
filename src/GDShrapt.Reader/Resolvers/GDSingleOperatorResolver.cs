using System;

namespace GDShrapt.Reader
{
    internal class GDSingleOperatorResolver : GDPattern
    {
        readonly Action<GDSingleOperatorType> _handler;

        public GDSingleOperatorResolver(Action<GDSingleOperatorType> handler)
        {
            _handler = handler;
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
                    _handler(GDSingleOperatorType.Not);
                    break;
                case "-":
                    _handler(GDSingleOperatorType.Negate);
                    break;
                case "~":
                    _handler(GDSingleOperatorType.BitwiseNegate);
                    break;
                default:
                    _handler(GDSingleOperatorType.Unknown);

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
            _handler(GDSingleOperatorType.Unknown);
            state.PopNode();
            state.PassLineFinish();
        }
    }
}
