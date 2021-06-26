namespace GDShrapt.Reader
{
    public sealed class GDFigureOpenBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => '{';

        public override GDSyntaxToken Clone()
        {
            return new GDFigureOpenBracket();
        }
    }
}
