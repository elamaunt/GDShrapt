namespace GDShrapt.Reader
{
    public sealed class GDAssign : GDPairToken, IGDStructureToken
    {
        public override char Char => '=';

        public override GDSyntaxToken Clone()
        {
            return new GDAssign();
        }
    }
}
