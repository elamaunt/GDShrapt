namespace GDShrapt.Reader
{
    public sealed class GDWhileKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "while";

        public override GDSyntaxToken Clone()
        {
            return new GDWhileKeyword();
        }
    }
}
