namespace GDShrapt.Reader
{
    public sealed class GDDollar : GDSingleCharToken
    {
        public override char Char => '$';

        public override GDSyntaxToken Clone()
        {
            return new GDDollar();
        }
    }
}
