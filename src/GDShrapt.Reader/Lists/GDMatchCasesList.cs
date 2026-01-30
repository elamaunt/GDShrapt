namespace GDShrapt.Reader
{
    public sealed class GDMatchCasesList : GDIntendedTokensList<GDMatchCaseDeclaration>
    {
        private int _lineIntendationThreshold;
        bool _completed;

        internal GDMatchCasesList(int lineIntendation)
        {
            _lineIntendationThreshold = lineIntendation;
        }

        public GDMatchCasesList()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.PushAndPass(new GDMatchCasesResolver(this, _lineIntendationThreshold), c);
                return;
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.PushAndPassNewLine(new GDMatchCasesResolver(this, _lineIntendationThreshold));
                return;
            }

            state.PopAndPassNewLine();
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDMatchCasesResolver(this, _lineIntendationThreshold));
                state.PassCarriageReturnChar();
                return;
            }

            state.PopAndPassCarriageReturnChar();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDMatchCasesList();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }
    }
}
