namespace GDShrapt.Reader
{
    public sealed class GDNewLine : GDSingleCharToken
    {
        public override char Char => '\n';

        public override GDSyntaxToken Clone()
        {
            return new GDNewLine();
        }

        public override int NewLinesCount => 1;
        public override int Length => 0;
    }
}
