namespace GDShrapt.Reader
{
    public sealed class GDColon : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => ':';

        public override GDSyntaxToken Clone()
        {
            return new GDColon();
        }
    }
}
