namespace GDShrapt.Reader
{
    public sealed class GDRightSlash : GDSingleCharToken
    {
        public override char Char => '/';

        public override GDSyntaxToken Clone()
        {
            return new GDRightSlash();
        }
    }
}
