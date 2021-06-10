namespace GDShrapt.Reader
{
    public abstract class GDStatement : GDNode
    {
        /// <summary>
        /// The count of '\t' before the statement
        /// </summary>
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
