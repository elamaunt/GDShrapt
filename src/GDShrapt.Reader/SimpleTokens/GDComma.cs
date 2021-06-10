namespace GDShrapt.Reader
{
    public sealed class GDOpenBracket : GDSingleCharToken, IGDStructureToken
    {
        public override char Char => '(';
    }
}
