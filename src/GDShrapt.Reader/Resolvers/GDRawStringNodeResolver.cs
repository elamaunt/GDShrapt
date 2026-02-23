namespace GDShrapt.Reader
{
    internal class GDRawStringNodeResolver : GDPatternResolver
    {
        new ITokenOrSkipReceiver<GDStringNode> Owner { get; }

        public GDRawStringNodeResolver(ITokenOrSkipReceiver<GDStringNode> owner)
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
            GDStringNode node;

            switch (pattern)
            {
                case "\"":
                    var dNode = new GDDoubleQuotasStringNode();
                    dNode.Parts = new GDStringPartsList(GDStringBoundingChar.DoubleQuotas, isRawString: true);
                    node = dNode;
                    break;
                case "'":
                    var sNode = new GDSingleQuotasStringNode();
                    sNode.Parts = new GDStringPartsList(GDStringBoundingChar.SingleQuotas, isRawString: true);
                    node = sNode;
                    break;
                case "\"\"\"":
                    var tdNode = new GDTripleDoubleQuotasStringNode();
                    tdNode.Parts = new GDStringPartsList(GDStringBoundingChar.TripleDoubleQuotas, isRawString: true);
                    node = tdNode;
                    break;
                case "'''":
                    var tsNode = new GDTripleSingleQuotasStringNode();
                    tsNode.Parts = new GDStringPartsList(GDStringBoundingChar.TripleSingleQuotas, isRawString: true);
                    node = tsNode;
                    break;
                default:
                    Owner.HandleReceivedTokenSkip();
                    if (pattern != null)
                        state.PassString(pattern);
                    return;
            }

            Owner.HandleReceivedToken(state.Push(node));

            if (pattern != null)
                state.PassString(pattern);
        }
    }
}
