namespace GDShrapt.Reader
{
    public sealed class GDDictionaryKeyword : GDKeyword
    {
        public override string Sequence => "Dictionary";

        public override GDSyntaxToken Clone()
        {
            return new GDDictionaryKeyword();
        }
    }
}
