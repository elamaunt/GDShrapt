namespace GDShrapt.Reader
{
    public sealed class GDLeftSlash : GDSingleCharToken
    {
        public override char Char => '\\';

        public override GDSyntaxToken Clone()
        {
            return new GDLeftSlash();
        }
    }
}
