namespace GDShrapt.Reader
{
    public sealed class GDSquareCloseBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => ']';

        public override GDSyntaxToken Clone()
        {
            return new GDSquareCloseBracket();
        }
    }
}
