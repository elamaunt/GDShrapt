using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public class GDIfStatement : GDStatement
    {
        public GDIfStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDExpression Condition { get; set; }
        public List<GDStatement> TrueStatements { get; } = new List<GDStatement>();
        public List<GDStatement> FalseStatements { get; } = new List<GDStatement>();

        private bool _expressionEnded;
        private bool _trueStatementsChecked;
        private bool _falseStatementsChecked;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Condition == null)
            {
                state.PushNode(new GDExpressionResolver(expr => Condition = expr));
                state.HandleChar(c);
                return;
            }

            if (!_expressionEnded)
            {
                if (c != ':')
                    return;

                _expressionEnded = true;
                return;
            }

            if (!_trueStatementsChecked)
            {
                _trueStatementsChecked = true;
                state.PushNode(new GDStatementResolver(LineIntendation + 1, expr => TrueStatements.Add(expr)));
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

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (!_trueStatementsChecked)
            {
                _trueStatementsChecked = true;
                state.PushNode(new GDStatementResolver(LineIntendation + 1, expr => TrueStatements.Add(expr)));
                return;
            }

            state.PopNode();
            state.FinishLine();
        }
    }
}