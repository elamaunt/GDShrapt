namespace GDShrapt.Reader
{
    public sealed class GDExportKeyword : GDSequenceToken, IGDKeywordToken
    {
        public override string Sequence => "export";

        public override GDSyntaxToken Clone()
        {
            return new GDExportKeyword();
        }
    }
}
