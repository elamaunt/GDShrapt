namespace GDShrapt.Reader
{
    public class GDSingleQuotas : GDSingleCharToken
    {
        public override char Char => '\'';

        public override GDSyntaxToken Clone()
        {
            return new GDSingleQuotas();
        }
    }
}
