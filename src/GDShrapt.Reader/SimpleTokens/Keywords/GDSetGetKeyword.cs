namespace GDShrapt.Reader
{
    public sealed class GDSetGetKeyword : GDKeyword
    {
        public override string Sequence => "setget";

        public override GDSyntaxToken Clone()
        {
            return new GDSetGetKeyword();
        }
    }
}
