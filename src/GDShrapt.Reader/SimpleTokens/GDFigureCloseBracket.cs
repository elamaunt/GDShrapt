namespace GDShrapt.Reader
{
    public sealed class GDFigureCloseBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => '}';

        public override GDSyntaxToken Clone()
        {
            return new GDFigureCloseBracket();
        }
    }
}
