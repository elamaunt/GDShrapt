namespace GDShrapt.Reader
{
    public sealed class GDSignalKeyword : GDKeyword
    {
        public override string Sequence => "signal";

        public override GDSyntaxToken Clone()
        {
            return new GDSignalKeyword();
        }
    }
}
