using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public class GDMatchCaseDeclaration : GDNode
    {
        readonly int _lineIntendation;
        bool _expressionEnded;
        bool _statementsChecked;

        public List<GDExpression> Conditions { get; } = new List<GDExpression>();
        public List<GDStatement> Statements { get; } = new List<GDStatement>();

        internal GDMatchCaseDeclaration(int lineIntendation)
        {
            _lineIntendation = lineIntendation;
        }

        public GDMatchCaseDeclaration()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == ':' || _expressionEnded)
            {
                _expressionEnded = true;
                return;
            }

            state.PushNode(new GDExpressionResolver(expr => Conditions.Add(expr)));

            if (c != ',')
                state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (_expressionEnded && !_statementsChecked)
            {
                _statementsChecked = true;
                state.PushNode(new GDStatementResolver(_lineIntendation + 1, expr => Statements.Add(expr)));
                return;
            }

            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $@"{string.Join(", ", Conditions.Select(x => x.ToString()))}:
    {string.Join("\n\t", Statements.Select(x => x.ToString()))}";
        }
    }
}
