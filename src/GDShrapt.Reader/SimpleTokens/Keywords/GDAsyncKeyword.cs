namespace GDShrapt.Reader
{
    public sealed class GDAsyncKeyword : GDKeyword
    {
        public override string Sequence => "async";

        public override GDSyntaxToken Clone()
        {
            return new GDAsyncKeyword();
        }
    }
}
