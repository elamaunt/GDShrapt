namespace GDShrapt.Reader
{
    public sealed class GDBreakPointKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "breakpoint";
    }
}
