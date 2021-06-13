namespace GDShrapt.Reader
{
    public sealed class GDAssign : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => '=';
    }
}
