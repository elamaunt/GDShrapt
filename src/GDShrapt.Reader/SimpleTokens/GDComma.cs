namespace GDShrapt.Reader
{
    public sealed class GDComma : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => ',';

        public override GDSyntaxToken Clone()
        {
            return new GDComma();
        }
    }
}
