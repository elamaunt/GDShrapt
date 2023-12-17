namespace GDShrapt.Reader
{
    public class GDTripleSingleQuotas : GDSequenceToken
    {
        public override string Sequence => "'''";

        public override GDSyntaxToken Clone()
        {
            return new GDTripleSingleQuotas();
        }
    }
}
