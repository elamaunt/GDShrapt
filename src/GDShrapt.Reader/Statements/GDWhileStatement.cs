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
                state.Push(new GDExpressionResolver(this));
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
                state.Push(statement);
                state.PassChar(c);
                return;
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (!_statementsChecked)
            {
                _statementsChecked = true;
                state.Push(new GDStatementResolver(this, LineIntendation + 1));
                return;
            }

            state.Pop();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $@"while {Condition}:
    {string.Join("\n\t", Statements.Select(x => x.ToString()))}";
        }
    }
}
