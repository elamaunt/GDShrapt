namespace GDShrapt.Reader
{
    public class GDTripleDoubleQuotas : GDSequenceToken
    {
        public override string Sequence => "\"\"\"";

        public override GDSyntaxToken Clone()
        {
            return new GDTripleDoubleQuotas();
        }
    }
}
