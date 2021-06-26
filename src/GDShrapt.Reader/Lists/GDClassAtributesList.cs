namespace GDShrapt.Reader
{
    public sealed class GDClassAtributesList : GDSeparatedList<GDClassAtribute, GDNewLine>, IClassAtributesReceiver
    {
        private int _lineIntendationThreshold;
        bool _completed;

        internal GDClassAtributesList(int lineIntendation)
        {
            _lineIntendationThreshold = lineIntendation;
        }

        public GDClassAtributesList()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDClassAtributesResolver(this, _lineIntendationThreshold));
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
                state.Push(new GDClassAtributesResolver(this, _lineIntendationThreshold));
                state.PassNewLine();
                return;
            }

            state.Pop();
            state.PassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDClassAtributesList();
        }

        void IClassAtributesReceiver.HandleReceivedToken(GDClassAtribute token)
        {
            ListForm.Add(token);
        }

        void IIntendationReceiver.HandleReceivedToken(GDIntendation token)
        {
            ListForm.Add(token);
        }
    }
}
