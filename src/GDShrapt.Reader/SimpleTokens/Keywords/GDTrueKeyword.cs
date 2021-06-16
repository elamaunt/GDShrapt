namespace GDShrapt.Reader
{
    public sealed class GDTrueKeyword : GDBoolKeyword
    {
        public override string Sequence => "true";

        public override bool Value => true;
    }
}
