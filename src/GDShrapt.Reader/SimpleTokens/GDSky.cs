namespace GDShrapt.Reader
{
    public sealed class GDSky : GDSingleCharToken
    {
        public override char Char => '^';

        public override GDSyntaxToken Clone()
        {
            return new GDSky();
        }
    }
}
