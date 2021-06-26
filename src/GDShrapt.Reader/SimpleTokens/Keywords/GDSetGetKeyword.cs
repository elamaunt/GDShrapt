namespace GDShrapt.Reader
{
    public sealed class GDSetGetKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "setget";

        public override GDSyntaxToken Clone()
        {
            return new GDSetGetKeyword();
        }
    }
}
