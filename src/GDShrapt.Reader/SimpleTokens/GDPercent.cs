namespace GDShrapt.Reader
{
    public sealed class GDPercent : GDSingleCharToken
    {
        public override char Char => '%';

        public override GDSyntaxToken Clone()
        {
            return new GDPercent();
        }
    }
}
