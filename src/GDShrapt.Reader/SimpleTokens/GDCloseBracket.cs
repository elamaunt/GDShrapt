namespace GDShrapt.Reader
{
    public sealed class GDCloseBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => ')';

        public override GDSyntaxToken Clone()
        {
            return new GDCloseBracket();
        }
    }
}
