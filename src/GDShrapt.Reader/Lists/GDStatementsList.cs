using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDStatementsList : GDIntendedTokensList<GDStatement>
    {
        bool _completed;

        internal GDStatementsList(int lineIntendation)
             : base(lineIntendation)
        {
        }

        public GDStatementsList()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDStatementsResolver(this, LineIntendationThreshold));
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
                state.Push(new GDStatementsResolver(this, LineIntendationThreshold));
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
            if (!beforeLine.HasValue)
            {
                return Form.Direct()
                      .OfType<GDVariableDeclarationStatement>()
                      .Select(x => x.Identifier)
                      .Where(x => x != null);
            }

            return Form.Direct()
                       .OfType<GDVariableDeclarationStatement>()
                       .Where(x => x.StartLine < beforeLine)
                       .Select(x => x.Identifier)
                       .Where(x => x != null);
        }
    }
}
