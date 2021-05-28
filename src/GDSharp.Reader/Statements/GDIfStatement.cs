using System.Collections.Generic;

namespace GDSharp.Reader
{
    public class GDIfStatement : GDStatement
    {
        public GDExpression Condition { get; set; }
        public List<GDStatement> TrueStatements { get; } = new List<GDStatement>();
        public List<GDStatement> FalseStatements { get; } = new List<GDStatement>();

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Condition == null)
            {
                state.PushNode(new GDExpressionResolver(expr => Condition = expr));
                state.HandleChar(c);
                return;
            }

            if (TrueStatements.Count == 0)
            {
                state.PushNode(new GDStatementResolver(expr => TrueStatements.Add(expr)));
                state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);

            // TODO: false branch
            /*if (FalseStatements.Count == 0)
            {
                state.PushNode(new GDStatementResolver(expr => FalseStatements.Add(expr)));
                state.HandleChar(c);
                return;
            }*/
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            // Ignore
        }
    }
}