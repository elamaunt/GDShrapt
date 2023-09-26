namespace GDShrapt.Reader
{
    internal class GDSetGetAccessorsResolver<T> : GDIntendedPatternResolver
        where T : IIntendedTokenOrSkipReceiver<GDAccessorDeclarationNode>
    {
        public new T Owner { get; }

        public GDSetGetAccessorsResolver(T owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
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
                "set:"
            };
        }

        protected override void PatternMatched(string pattern, GDReadingState state)
        {
            switch (pattern)
            {
                case "set":
                    {
                        // TODO: check the colon
                        var accessor = new GDSetAccessorMethodDeclarationNode(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        break;
                    }
                case "set=":
                    {
                        var accessor = new GDSetAccessorMethodDeclarationNode(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        break;
                    }
                case "set:":
                    {
                        var accessor = new GDSetAccessorBodyDeclarationNode(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        break;
                    }
                case "get":
                    {
                        // TODO: check the colon
                        var accessor = new GDGetAccessorMethodDeclarationNode(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        break;
                    }
                case "get=":
                    {
                        var accessor = new GDGetAccessorMethodDeclarationNode(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        break;
                    }
                case "get:":
                    {
                        var accessor = new GDGetAccessorBodyDeclarationNode(LineIntendationThreshold);
                        Owner.HandleReceivedToken(accessor);
                        break;
                    }
                default:
                    Owner.HandleReceivedTokenSkip();

                    if (pattern != null)
                    {
                        for (int i = 0; i < pattern.Length; i++)
                            state.PassChar(pattern[i]);
                    }
                    break;
            }
        }

        internal override void ForceComplete(GDReadingState state)
        {
            Owner.HandleReceivedTokenSkip();
            base.ForceComplete(state);
        }
    }
}
