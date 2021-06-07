using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDWhileStatement : GDStatement
    {
        bool _expressionEnded;
        bool _statementsChecked;
        
        public GDExpression Condition { get; set; }
        public List<GDStatement> Statements { get; } = new List<GDStatement>();

        internal GDWhileStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDWhileStatement()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Condition == null)
            {
                state.PushNode(new GDExpressionResolver(expr => Condition = expr));
                state.PassChar(c);
                return;
            }

            if (!_expressionEnded)
            {
                if (c != ':')
                    return;

                _expressionEnded = true;
                return;
            }

            if (!_statementsChecked)
            {
                _statementsChecked = true;
                var statement = new GDExpressionStatement(LineIntendation + 1);
                Statements.Add(statement);
                state.PushNode(statement);
                state.PassChar(c);
                return;
            }

            state.PopNode();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (!_statementsChecked)
            {
                _statementsChecked = true;
                state.PushNode(new GDStatementResolver(LineIntendation + 1, expr => Statements.Add(expr)));
                return;
            }

            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $@"while {Condition}:
    {string.Join("\n\t", Statements.Select(x => x.ToString()))}";
        }
    }
}
