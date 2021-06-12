namespace GDShrapt.Reader
{
    public class GDStatementsList : GDSeparatedList<GDStatement, GDNewLine>, IStatementsReceiver
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
                state.Push(new GDStatementResolver(this, _lineIntendationThreshold));
                state.PassChar(c);
                return;
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDStatementResolver(this, _lineIntendationThreshold));
                state.PassLineFinish();
                return;
            }

            state.Pop();
            state.PassLineFinish();
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
