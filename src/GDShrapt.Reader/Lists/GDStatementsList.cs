using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDStatementsList : GDSeparatedList<GDStatement, GDNewLine>, IStatementsReceiver
    {
        private int _lineIntendationThreshold;
        bool _completed;

        internal GDStatementsList(int lineIntendation)
        {
            _lineIntendationThreshold = lineIntendation;
        }

        public GDStatementsList()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDStatementsResolver(this, _lineIntendationThreshold));
                state.PassChar(c);
                return;
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDStatementsResolver(this, _lineIntendationThreshold));
                state.PassNewLine();
                return;
            }

            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDStatementsList();
        }

        public override IEnumerable<GDIdentifier> GetMethodScopeDeclarations(int? beforeLine = null)
        {
            if (beforeLine.HasValue)
            {
                return Form.Direct()
                      .OfType<GDVariableDeclarationStatement>()
                      .Select(x => x.Identifier);
            }

            return Form.Direct()
                       .OfType<GDVariableDeclarationStatement>()
                       .Where(x => x.StartLine < beforeLine)
                       .Select(x => x.Identifier);
        }

        void IStatementsReceiver.HandleReceivedToken(GDStatement token)
        {
            ListForm.Add(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            ListForm.Add(token);
        }

        void IIntendationReceiver.HandleReceivedToken(GDIntendation token)
        {
            ListForm.Add(token);
        }
    }
}
