using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public abstract class GDStatement : GDNode
    {
        //public List<GDComment> CommentsBefore { get; } = new List<GDComment>();

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
