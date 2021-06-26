namespace GDShrapt.Reader
{
    public sealed class GDOnreadyKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "onready";

        public override GDSyntaxToken Clone()
        {
            return new GDOnreadyKeyword();
        }
    }
}
