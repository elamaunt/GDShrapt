namespace GDShrapt.Reader
{
    public sealed class GDPassExpression : GDExpression,
        IKeywordReceiver<GDPassKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Pass);

        internal GDPassKeyword PassKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            Pass,
            Completed
        }

        readonly GDTokensForm<State, GDPassKeyword> _form;
        internal override GDTokensForm Form => _form;
        public GDPassExpression()
        {
            _form = new GDTokensForm<State, GDPassKeyword>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.Pass)
                state.Push(new GDKeywordResolver<GDPassKeyword>(this));
            else
                state.Pop();

            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        void IKeywordReceiver<GDPassKeyword>.HandleReceivedToken(GDPassKeyword token)
        {
            if (_form.State == State.Pass)
            {
                _form.State = State.Completed;
                PassKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDPassKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Pass)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}

