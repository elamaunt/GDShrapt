namespace GDShrapt.Reader
{
    public sealed class GDClassAtributesList : GDIntendedTokensList<GDClassAtribute>,
        ITokenReceiver<GDClassAtribute>
    {
        bool _completed;

        internal GDClassAtributesList(int lineIntendation) 
            : base(lineIntendation)
        {
        }

        public GDClassAtributesList()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDClassAtributesResolver(this, LineIntendationThreshold));
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
                state.Push(new GDClassAtributesResolver(this, LineIntendationThreshold));
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

        void ITokenReceiver<GDClassAtribute>.HandleReceivedToken(GDClassAtribute token)
        {
            ListForm.Add(token);
        }
    }
}
