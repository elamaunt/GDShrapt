namespace GDShrapt.Reader
{
    public sealed class GDSignalKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "signal";

        public override GDSyntaxToken Clone()
        {
            return new GDSignalKeyword();
        }
    }
}
