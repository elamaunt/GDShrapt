using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDMatchStatement : GDStatement
    {
        bool _expressionEnded;
        bool _casesChecked;

        public GDExpression Value { get; set; }
        public List<GDMatchCaseDeclaration> Cases { get; } = new List<GDMatchCaseDeclaration>();

        internal GDMatchStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDMatchStatement()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Value == null)
            {
                state.PushNode(new GDExpressionResolver(expr => Value = expr));
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
            if (!_casesChecked)
            {
                _casesChecked = true;
                state.PushNode(new GDMatchCaseResolver(LineIntendation + 1, @case => Cases.Add(@case)));
                return;
            }

            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $@"match {Value}:
    {string.Join("\n\t", Cases.Select(x => x.ToString()))}";
        }
    }
}