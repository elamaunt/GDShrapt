namespace GDShrapt.Reader
{
    public sealed class GDBoolExpression : GDExpression,
        ITokenOrSkipReceiver<GDTrueKeyword>,
        ITokenOrSkipReceiver<GDFalseKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);

        public GDBoolKeyword BoolKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public bool? Value => BoolKeyword?.Value;

        enum State
        {
            True,
            False,
            Completed
        }

        readonly GDTokensForm<State, GDBoolKeyword> _form;
        public override GDTokensForm Form => _form;
        public GDBoolExpression()
        {
            _form = new GDTokensForm<State, GDBoolKeyword>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.True)
            {
                this.ResolveKeyword<GDTrueKeyword>(c, state);
                return;
            }

            if (_form.State == State.False)
            {
                this.ResolveKeyword<GDFalseKeyword>(c, state);
                return;
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDBoolExpression();
        }

        void ITokenReceiver<GDTrueKeyword>.HandleReceivedToken(GDTrueKeyword token)
        {
            if (_form.State != State.Completed)
            {
                _form.State = State.Completed;
                BoolKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDTrueKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State != State.Completed)
            {
                _form.State = State.False;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDFalseKeyword>.HandleReceivedToken(GDFalseKeyword token)
        {
            if (_form.State != State.Completed)
            {
                _form.State = State.Completed;
                BoolKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDFalseKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State != State.Completed)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
