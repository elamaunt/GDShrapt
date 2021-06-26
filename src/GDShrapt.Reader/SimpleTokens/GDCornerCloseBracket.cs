namespace GDShrapt.Reader
{
    public class GDCornerCloseBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => '>';

        public override GDSyntaxToken Clone()
        {
            return new GDCornerCloseBracket();
        }
    }
}
