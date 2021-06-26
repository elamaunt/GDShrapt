namespace GDShrapt.Reader
{
    public sealed class GDDefaultToken : GDSingleCharToken
    {
        public override char Char => '_';

        public override GDSyntaxToken Clone()
        {
            return new GDDefaultToken();
        }
    }
}
