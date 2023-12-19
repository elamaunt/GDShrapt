namespace GDShrapt.Reader
{
    internal class GDStringNodeResolver : GDPatternResolver
    {
        new ITokenOrSkipReceiver<GDStringNode> Owner { get; }

        public GDStringNodeResolver(ITokenOrSkipReceiver<GDStringNode> owner)
            : base(owner)
        {
            Owner = owner;
        }

        public override string[] GeneratePatterns()
        {
            return new string[] 
            {
                "\"",
                "'",
                "\"\"\"",
                "'''",
            };
        }

        protected override void PatternMatched(string pattern, GDReadingState state)
        {
            switch (pattern)
            {
                case "\"":
                    Owner.HandleReceivedToken(state.Push(new GDDoubleQuotasStringNode()));
                    break;
                case "'":
                    Owner.HandleReceivedToken(state.Push(new GDSingleQuotasStringNode()));
                    break;
                case "\"\"\"":
                    Owner.HandleReceivedToken(state.Push(new GDMultilineDoubleQuotasStringNode()));
                    break;
                case "'''":
                    Owner.HandleReceivedToken(state.Push(new GDMultilineSingleQuotasStringNode()));
                    break;
                default:
                    Owner.HandleReceivedTokenSkip();
                    break;
            }

            if (pattern != null)
                state.PassString(pattern);
        }
    }
}
