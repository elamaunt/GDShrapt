namespace GDShrapt.Reader
{
    public sealed class GDPassExpression : GDExpression,
        ITokenOrSkipReceiver<GDPassKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Pass);

        public GDPassKeyword PassKeyword
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
        public override GDTokensForm Form => _form;
        public GDPassExpression()
        {
            _form = new GDTokensForm<State, GDPassKeyword>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.IsOrLowerState(State.Pass))
                state.Push(new GDKeywordResolver<GDPassKeyword>(this));
            else
                state.Pop();

            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDPassExpression();
        }

        void ITokenReceiver<GDPassKeyword>.HandleReceivedToken(GDPassKeyword token)
        {
            if (_form.IsOrLowerState(State.Pass))
            {
                _form.State = State.Completed;
                PassKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDPassKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Pass))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}

