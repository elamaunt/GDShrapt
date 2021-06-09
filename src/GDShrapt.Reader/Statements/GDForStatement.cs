using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDForStatement : GDStatement
    {
        bool _checkedInKeyword;
        bool _expressionEnded;
        bool _statementsChecked;

        public GDIdentifier Variable { get; set; }
        public GDExpression Collection { get; set; }

        public List<GDStatement> Statements { get; } = new List<GDStatement>();

        internal GDForStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDForStatement()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Variable == null)
            {
                state.Push(Variable = new GDIdentifier());
                state.PassChar(c);
                return;
            }

            if(!_checkedInKeyword)
            {
                state.Push(new GDStaticKeywordResolver(this));
                state.PassChar(c);
                return;
            }

            if (Collection == null)
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
            return $@"for {Variable} in {Collection}:
    {string.Join("\n\t", Statements.Select(x => x.ToString()))}";
        }
    }
}
