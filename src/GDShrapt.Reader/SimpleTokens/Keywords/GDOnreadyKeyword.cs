namespace GDShrapt.Reader
{
    public sealed class GDOnreadyKeyword : GDKeyword
    {
        public override string Sequence => "onready";

        public override GDSyntaxToken Clone()
        {
            return new GDOnreadyKeyword();
        }
    }
}
