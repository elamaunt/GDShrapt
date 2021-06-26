namespace GDShrapt.Reader
{
    public sealed class GDSemiColon : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => ';';

        public override GDSyntaxToken Clone()
        {
            return new GDSemiColon();
        }
    }
}
