namespace GDShrapt.Reader
{
    public abstract class GDStatement : GDNode
    {
        internal int LineIntendation { get; }

        internal GDStatement(int lineIntendation)
        {
            LineIntendation = lineIntendation;
        }

        public GDStatement()
        {

        }
    }
}
