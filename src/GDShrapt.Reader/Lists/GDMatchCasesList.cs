namespace GDShrapt.Reader
{
    public sealed class GDMatchCasesList : GDSeparatedList<GDMatchCaseDeclaration, GDNewLine>, IMatchCaseReceiver
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
                state.Push(new GDMatchCaseResolver(this, _lineIntendationThreshold));
                state.PassChar(c);
                return;
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDMatchCaseResolver(this, _lineIntendationThreshold));
                state.PassNewLine();
                return;
            }

            state.Pop();
            state.PassNewLine();
        }

        void IMatchCaseReceiver.HandleReceivedToken(GDMatchCaseDeclaration token)
        {
            ListForm.Add(token);
        }

        void IIntendationReceiver.HandleReceivedToken(GDIntendation token)
        {
            ListForm.Add(token);
        }
    }
}
