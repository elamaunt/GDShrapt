namespace GDShrapt.Reader
{
    public sealed class GDExpressionStatement : GDStatement,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDSemiColon>
    {
        public GDExpression Expression
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDSemiColon SemiColon
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public enum State
        {
            Expression,
            SemiColon,
            Completed
        }

        readonly GDTokensForm<State, GDExpression, GDSemiColon> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDExpression, GDSemiColon> TypedForm => _form;

        internal GDExpressionStatement(int lineIntendation)
            : base(lineIntendation)
        {
            _form = new GDTokensForm<State, GDExpression, GDSemiColon>(this);
        }

        public GDExpressionStatement()
        {
            _form = new GDTokensForm<State, GDExpression, GDSemiColon>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Expression:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveExpression(c, state);
                    break;
                case State.SemiColon:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveSemiColon(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDExpressionStatement();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.SemiColon;
                Expression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDSemiColon>.HandleReceivedToken(GDSemiColon token)
        {
            if (_form.IsOrLowerState(State.SemiColon))
            {
                _form.State = State.Completed;
                SemiColon = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSemiColon>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.SemiColon))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}