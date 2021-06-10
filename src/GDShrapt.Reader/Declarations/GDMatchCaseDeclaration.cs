using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDMatchCaseDeclaration : GDNode, IExpressionsReceiver, IStatementsReceiver
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

            state.Push(new GDExpressionResolver(this));

            if (c != ',')
                state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (_expressionEnded && !_statementsChecked)
            {
                _statementsChecked = true;
                state.Push(new GDStatementResolver(this, _lineIntendation + 1));
                return;
            }

            state.Pop();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $@"{string.Join(", ", Conditions.Select(x => x.ToString()))}:
    {string.Join("\n\t", Statements.Select(x => x.ToString()))}";
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            throw new System.NotImplementedException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDComment token)
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDSpace token)
        {
            throw new System.NotImplementedException();
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            throw new System.NotImplementedException();
        }

        void IStatementsReceiver.HandleReceivedToken(GDStatement token)
        {
            throw new System.NotImplementedException();
        }
    }
}
