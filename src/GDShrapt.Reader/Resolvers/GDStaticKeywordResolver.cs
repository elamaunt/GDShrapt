namespace GDShrapt.Reader
{
    internal class GDStaticKeywordResolver : GDPatternResolver
    {
        public GDStaticKeywordResolver(ITokensContainer owner)
            : base(owner)
        {
        }

        public override string[] GeneratePatterns()
        {
            return new string[]
            {
                "in",
                "setget",
                "if",
                "else",
                "in"
            };
        }

        protected override void PatternMatched(string pattern, GDReadingState state)
        {
            switch (pattern)
            {
                case "in":
                    Append(new GDInKeyword());
                    return;
                case "if":
                    Append(new GDIfKeyword());
                    return;
                case "else":
                    Append(new GDElseKeyword());
                    return;
                case "setget":
                    Append(new GDSetGetKeyword());
                    return;
                default:
                    throw new GDInvalidReadingStateExeption($"Unhandled pattern match '{pattern}'");
            }
        }
    }
}
