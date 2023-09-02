namespace GDShrapt.Reader
{
    public sealed class GDInKeyword : GDKeyword
    {
        public override string Sequence => "in";

        public override GDSyntaxToken Clone()
        {
            return new GDInKeyword();
        }
    }
}
