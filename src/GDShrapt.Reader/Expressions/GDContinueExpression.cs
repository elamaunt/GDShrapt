namespace GDShrapt.Reader
{
    public sealed class GDContinueExpression : GDExpression,
        IKeywordReceiver<GDContinueKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Continue);

        internal GDContinueKeyword ContinueKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            Continue,
            Completed
        }

        readonly GDTokensForm<State, GDContinueKeyword> _form;
        internal override GDTokensForm Form => _form;

        public GDContinueExpression()
        {
            _form = new GDTokensForm<State, GDContinueKeyword>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.Continue)
                state.Push(new GDKeywordResolver<GDContinueKeyword>(this));
            else
                state.Pop();

            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDContinueExpression();
        }

        void IKeywordReceiver<GDContinueKeyword>.HandleReceivedToken(GDContinueKeyword token)
        {
            if (_form.State == State.Continue)
            {
                _form.State = State.Completed;
                ContinueKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDContinueKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Continue)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}