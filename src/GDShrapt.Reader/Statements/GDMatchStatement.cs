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
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (!_casesChecked)
            {
                _casesChecked = true;
                state.Push(new GDMatchCaseResolver(this, LineIntendation + 1));
                return;
            }

            state.Pop();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $@"match {Value}:
    {string.Join("\n\t", Cases.Select(x => x.ToString()))}";
        }
    }
}