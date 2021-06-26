namespace GDShrapt.Reader
{
    public class GDSquareOpenBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => '[';

        public override GDSyntaxToken Clone()
        {
            return new GDSquareOpenBracket();
        }
    }
}
