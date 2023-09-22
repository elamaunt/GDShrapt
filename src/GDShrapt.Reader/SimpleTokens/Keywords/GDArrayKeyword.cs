namespace GDShrapt.Reader
{
    public sealed class GDArrayKeyword : GDKeyword
    {
        public override string Sequence => "Array";

        public override GDSyntaxToken Clone()
        {
            return new GDAsyncKeyword();
        }
    }
}