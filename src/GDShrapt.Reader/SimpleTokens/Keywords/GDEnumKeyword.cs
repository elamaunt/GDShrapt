namespace GDShrapt.Reader
{
    public sealed class GDEnumKeyword : GDKeyword
    {
        public override string Sequence => "enum";

        public override GDSyntaxToken Clone()
        {
            return new GDEnumKeyword();
        }
    }
}
