namespace GDShrapt.Reader
{
    public sealed class GDElifKeyword : GDKeyword
    {
        public override string Sequence => "elif";

        public override GDSyntaxToken Clone()
        {
            return new GDElifKeyword();
        }
    }
}
