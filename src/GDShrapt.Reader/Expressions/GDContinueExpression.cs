namespace GDShrapt.Reader
{
    public sealed class GDContinueExpression : GDExpression,
        ITokenOrSkipReceiver<GDContinueKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Continue);

        public GDContinueKeyword ContinueKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            Continue,
            Completed
        }

        readonly GDTokensForm<State, GDContinueKeyword> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDContinueKeyword> TypedForm => _form;

        public GDContinueExpression()
        {
            _form = new GDTokensForm<State, GDContinueKeyword>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.IsOrLowerState(State.Continue))
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

        void ITokenReceiver<GDContinueKeyword>.HandleReceivedToken(GDContinueKeyword token)
        {
            if (_form.IsOrLowerState(State.Continue))
            {
                _form.State = State.Completed;
                ContinueKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDContinueKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Continue))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}