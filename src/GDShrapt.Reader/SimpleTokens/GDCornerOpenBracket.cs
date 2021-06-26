namespace GDShrapt.Reader
{
    public class GDCornerOpenBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => '<';

        public override GDSyntaxToken Clone()
        {
            return new GDCornerOpenBracket();
        }
    }
}
