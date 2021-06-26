namespace GDShrapt.Reader
{
    public sealed class GDPoint : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => '.';

        public override GDSyntaxToken Clone()
        {
            return new GDPoint();
        }
    }
}
