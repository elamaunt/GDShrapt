namespace GDShrapt.Reader
{
    public sealed class GDSetKeyword : GDKeyword
    {
        public override string Sequence => "set";

        public override GDSyntaxToken Clone()
        {
            return new GDSetKeyword();
        }
    }
}
