namespace GDShrapt.Reader
{
    public sealed class GDExportKeyword : GDKeyword
    {
        public override string Sequence => "export";

        public override GDSyntaxToken Clone()
        {
            return new GDExportKeyword();
        }
    }
}
