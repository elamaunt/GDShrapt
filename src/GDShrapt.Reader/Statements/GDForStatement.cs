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
                state.SetReadingToken(Variable = new GDIdentifier());
                state.PassChar(c);
                return;
            }

            if(!_checkedInKeyword)
            {
                state.PushNode(new GDStaticKeywordResolver("in ", x => _checkedInKeyword = x));
                state.PassChar(c);
                return;
            }

            if (Collection == null)
            {
                state.PushNode(new GDExpressionResolver(expr => Collection = expr));
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
            return $@"for {Variable} in {Collection}:
    {string.Join("\n\t", Statements.Select(x => x.ToString()))}";
        }
    }
}
