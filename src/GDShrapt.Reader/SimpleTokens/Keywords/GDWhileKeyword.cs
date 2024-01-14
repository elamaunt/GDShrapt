namespace GDShrapt.Reader
{
    public sealed class GDWhileKeyword : GDKeyword
    {
        public override string Sequence => "while";

        public override GDSyntaxToken Clone()
        {
            return new GDWhileKeyword();
        }
    }
}
