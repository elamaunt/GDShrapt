using System;

namespace GDShrapt.Reader
{
    internal class GDSingleOperatorResolver : GDPattern
    {
        readonly Action<GDSingleOperatorType, GDComment> _handler;

        public GDSingleOperatorResolver(Action<GDSingleOperatorType, GDComment> handler)
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
                    _handler(GDSingleOperatorType.Not, EndLineComment);
                    break;
                case "-":
                    _handler(GDSingleOperatorType.Negate, EndLineComment);
                    break;
                case "~":
                    _handler(GDSingleOperatorType.BitwiseNegate, EndLineComment);
                    break;
                default:
                    _handler(GDSingleOperatorType.Unknown, EndLineComment);

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
            _handler(GDSingleOperatorType.Unknown, EndLineComment);
            state.PopNode();
            state.PassLineFinish();
        }
    }
}
