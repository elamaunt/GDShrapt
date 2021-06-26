namespace GDShrapt.Reader
{
    public class GDSquareCloseBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => ']';

        public override GDSyntaxToken Clone()
        {
            return new GDSquareCloseBracket();
        }
    }
}
