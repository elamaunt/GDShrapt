namespace GDShrapt.Reader
{
    public sealed class GDSquareOpenBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => '[';

        public override GDSyntaxToken Clone()
        {
            return new GDSquareOpenBracket();
        }
    }
}
