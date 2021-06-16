namespace GDShrapt.Reader
{
    public sealed class GDFalseKeyword : GDBoolKeyword
    {
        public override string Sequence => "false";
        public override bool Value => false;
    }
}
