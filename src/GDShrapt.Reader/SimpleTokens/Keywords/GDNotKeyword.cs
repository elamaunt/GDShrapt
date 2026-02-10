namespace GDShrapt.Reader
{
    public sealed class GDNotKeyword : GDKeyword
    {
        public override string Sequence => "not";

        public override GDSyntaxToken Clone()
        {
            return new GDNotKeyword();
        }
    }
}
