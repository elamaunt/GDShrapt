namespace GDShrapt.Reader
{
    public sealed class GDBoolExpression : GDExpression,
        IKeywordReceiver<GDTrueKeyword>,
        IKeywordReceiver<GDFalseKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);

        internal GDBoolKeyword BoolKeyword
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

        readonly GDTokensForm<State, GDBoolKeyword> _form = new GDTokensForm<State, GDBoolKeyword>();
        internal override GDTokensForm Form => _form;

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

        void IKeywordReceiver<GDTrueKeyword>.HandleReceivedToken(GDTrueKeyword token)
        {
            if (_form.State != State.Completed)
            {
                _form.State = State.Completed;
                BoolKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDTrueKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State != State.Completed)
            {
                _form.State = State.False;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDFalseKeyword>.HandleReceivedToken(GDFalseKeyword token)
        {
            if (_form.State != State.Completed)
            {
                _form.State = State.Completed;
                BoolKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDFalseKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State != State.Completed)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
