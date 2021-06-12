namespace GDShrapt.Reader
{
    public abstract class GDStatement : GDIntendedNode
    {
        /// <summary>
        /// The count of '\t' before the statement
        /// </summary>
        internal int LineIntendation { get; }


        /// <summary>
        /// Internal constructor to parse intendation incerement properly
        /// </summary>
        /// <param name="lineIntendation"></param>
        internal GDStatement(int lineIntendation)
        {
            LineIntendation = lineIntendation;
        }

        public GDStatement()
        {
        }
    }
}
