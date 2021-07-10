namespace GDShrapt.Reader
{
    public sealed class GDBreakExpression : GDExpression,
        ITokenOrSkipReceiver<GDBreakKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Break);

        public GDBreakKeyword BreakKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            Break,
            Completed
        }

        readonly GDTokensForm<State, GDBreakKeyword> _form;
        public override GDTokensForm Form => _form;
        public GDBreakExpression()
        {
            _form = new GDTokensForm<State, GDBreakKeyword>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.Break)
                state.Push(new GDKeywordResolver<GDBreakKeyword>(this));
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
            return new GDBreakExpression();
        }

        void ITokenReceiver<GDBreakKeyword>.HandleReceivedToken(GDBreakKeyword token)
        {
            if (_form.State == State.Break)
            {
                _form.State = State.Completed;
                BreakKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDBreakKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Break)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
