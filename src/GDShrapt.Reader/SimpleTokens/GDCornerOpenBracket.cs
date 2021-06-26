namespace GDShrapt.Reader
{
    public sealed class GDCornerOpenBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => '<';

        public override GDSyntaxToken Clone()
        {
            return new GDCornerOpenBracket();
        }
    }
}
