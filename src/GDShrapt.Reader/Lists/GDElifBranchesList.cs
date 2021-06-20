namespace GDShrapt.Reader
{
    public sealed class GDElifBranchesList : GDSeparatedList<GDElifBranch, GDNewLine>,
        IElifBranchReceiver
    {
        private int _lineIntendationThreshold;
        bool _completed;

        internal GDElifBranchesList(int lineIntendation)
        {
            _lineIntendationThreshold = lineIntendation;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.PushAndPass(new GDElifResolver(this, _lineIntendationThreshold), c);
                return;
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDElifResolver(this, _lineIntendationThreshold));
                state.PassNewLine();
                return;
            }

            state.PopAndPassNewLine();
        }

        void IElifBranchReceiver.HandleReceivedToken(GDElifBranch token)
        {
            ListForm.Add(token);
        }

        void IIntendationReceiver.HandleReceivedToken(GDIntendation token)
        {
            ListForm.Add(token);
        }
    }
}
