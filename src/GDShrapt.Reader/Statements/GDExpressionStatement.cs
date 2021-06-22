namespace GDShrapt.Reader
{
    public sealed class GDExpressionStatement : GDStatement,
        IExpressionsReceiver,
        ITokenReceiver<GDSemiColon>
    {
        public GDExpression Expression
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        internal GDSemiColon SemiColon
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        enum State
        {
            Expression,
            SemiColon,
            Completed
        }

        readonly GDTokensForm<State, GDExpression, GDSemiColon> _form;
        internal override GDTokensForm Form => _form;

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
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveExpression(c, state);
                    break;
                case State.SemiColon:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveSemiColon(c, state);
                    break;
                default:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveInvalidToken(c, state, x => !x.IsSpace());
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Expression)
            {
                _form.State = State.SemiColon;
                Expression = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.Expression)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDSemiColon>.HandleReceivedToken(GDSemiColon token)
        {
            if (_form.State == State.SemiColon)
            {
                _form.State = State.Completed;
                SemiColon = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDSemiColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.SemiColon)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}