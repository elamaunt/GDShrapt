using System;

namespace GDShrapt.Reader
{
    internal class GDSingleOperatorResolver : GDPatternResolver
    {
        public new ISingleOperatorReceiver Owner { get; }
        public GDSingleOperatorResolver(ISingleOperatorReceiver owner)
            : base(owner)
        {
            Owner = owner;
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
                    Owner.HandleSingleOperatorSkip();

                    if (pattern != null)
                    {
                        for (int i = 0; i < pattern.Length; i++)
                            state.PassChar(pattern[i]);
                    }
                    break;
            }
        }

        void Append(GDSingleOperatorType operatorType)
        {
            Owner.HandleReceivedToken(new GDSingleOperator() { OperatorType = operatorType });
        }

        internal override void ForceComplete(GDReadingState state)
        {
            Owner.HandleSingleOperatorSkip();
            base.ForceComplete(state);
        }
    }
}
