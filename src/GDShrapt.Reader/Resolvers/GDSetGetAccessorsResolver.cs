namespace GDShrapt.Reader
{
    internal class GDSetGetAccessorsResolver<T> : GDIntendedPatternResolver
        where T : IIntendedTokenOrSkipReceiver<GDAccessorDeclaration>
    {
        public new T Owner { get; }

        public GDSetGetAccessorsResolver(T owner, bool allowZeroIntendationOnFirstLine, int lineIntendation)
            : base(owner, lineIntendation)
        {
            AllowZeroIntendationOnFirstLine = allowZeroIntendationOnFirstLine;
            Owner = owner;
        }

        public override string[] GeneratePatterns()
        {
            return new string[] 
            { 
                "get",
                "set",

                "get=",
                "set=",

                "get:",
                "set("
            };
        }

        protected override void PatternMatched(string pattern, GDReadingState state)
        {
            if (pattern != null)
                SendIntendationTokensToOwner();

            switch (pattern)
            {
                case "set":
                    {
                        // TODO: check the colon
                        var accessor = new GDSetAccessorMethodDeclaration(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        state.Push(accessor);
                        break;
                    }
                case "set=":
                    {
                        var accessor = new GDSetAccessorMethodDeclaration(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        state.Push(accessor);
                        break;
                    }
                case "set(":
                    {
                        // TODO: check spaces
                        var accessor = new GDSetAccessorBodyDeclaration(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        state.Push(accessor);
                        break;
                    }
                case "get":
                    {
                        // TODO: check the colon
                        var accessor = new GDGetAccessorMethodDeclaration(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        state.Push(accessor);
                        break;
                    }
                case "get=":
                    {
                        var accessor = new GDGetAccessorMethodDeclaration(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        state.Push(accessor);
                        break;
                    }
                case "get:":
                    {
                        var accessor = new GDGetAccessorBodyDeclaration(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        state.Push(accessor);
                        break;
                    }
                default:
                    Owner.HandleReceivedTokenSkip();
                    break;
            }

            if (pattern != null)
            {
                for (int i = 0; i < pattern.Length; i++)
                    state.PassChar(pattern[i]);
            }
        }
    }
}
