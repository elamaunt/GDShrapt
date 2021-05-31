using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public class GDMatchCaseDeclaration : GDNode
    {
        readonly int _lineIntendation;
        bool _expressionEnded;
        bool _statementsChecked;

        public GDExpression Condition { get; set; }
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
            return $@"{Condition}:
    {string.Join("\n\t", Statements.Select(x => x.ToString()))}";
        }
    }
}
