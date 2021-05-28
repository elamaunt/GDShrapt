using System;

namespace GDShrapt.Reader
{
    public class GDSingleOperatorResolver : GDPattern
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
                "!"
            };

        }

        protected override void PatternMatched(string pattern)
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
                default:
                    _handler(GDSingleOperatorType.Unknown);
                    break;
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            _handler(GDSingleOperatorType.Unknown);
            state.PopNode();
            state.FinishLine();
        }
    }
}
