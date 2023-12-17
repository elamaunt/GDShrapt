namespace GDShrapt.Reader
{
    public class GDDoubleQuotas : GDSingleCharToken
    {
        public override char Char => '"';

        public override GDSyntaxToken Clone()
        {
            return new GDDoubleQuotas();
        }
    }
}
