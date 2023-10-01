namespace GDShrapt.Reader
{
    public sealed class GDMatchDefaultOperatorExpression : GDExpression,
        ITokenOrSkipReceiver<GDDefaultToken>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.DefaultOperator);

        public GDDefaultToken DefaultToken
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            Default,
            Completed
        }

        readonly GDTokensForm<State, GDDefaultToken> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDDefaultToken> TypedForm => _form;
        public GDMatchDefaultOperatorExpression()
        {
            _form = new GDTokensForm<State, GDDefaultToken>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.IsOrLowerState(State.Default))
                state.Push(new GDSingleCharTokenResolver<GDDefaultToken>(this));
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
            return new GDMatchDefaultOperatorExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDDefaultToken>.HandleReceivedToken(GDDefaultToken token)
        {
            if (_form.IsOrLowerState(State.Default))
            {
                _form.State = State.Completed;
                DefaultToken = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDDefaultToken>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Default))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
