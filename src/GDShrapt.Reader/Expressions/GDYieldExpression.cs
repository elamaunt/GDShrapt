namespace GDShrapt.Reader
{
    public sealed class GDYieldExpression : GDExpression,
        IKeywordReceiver<GDYieldKeyword>,
        ITokenReceiver<GDOpenBracket>,
        IExpressionsReceiver,
        ITokenReceiver<GDCloseBracket>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Yield);

        internal GDYieldKeyword YieldKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        internal GDOpenBracket OpenBracket
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExpression Expression
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        internal GDCloseBracket CloseBracket
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        enum State
        {
            Yield,
            OpenBracket,
            Expression,
            CloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDYieldKeyword, GDOpenBracket, GDExpression, GDCloseBracket> _form;
        internal override GDTokensForm Form => _form;
        public GDYieldExpression()
        {
            _form = new GDTokensForm<State, GDYieldKeyword, GDOpenBracket, GDExpression, GDCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Yield:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveKeyword(c, state);
                    break;
                case State.OpenBracket:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveOpenBracket(c, state);
                    break;
                case State.Expression:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveExpression(c, state);
                    break;
                case State.CloseBracket:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveCloseBracket(c, state);
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

        void IKeywordReceiver<GDYieldKeyword>.HandleReceivedToken(GDYieldKeyword token)
        {
            if (_form.State == State.Yield)
            {
                _form.State = State.OpenBracket;
                YieldKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDYieldKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Yield)
            {
                _form.State = State.OpenBracket;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
        void ITokenReceiver<GDOpenBracket>.HandleReceivedToken(GDOpenBracket token)
        {
            if (_form.State == State.OpenBracket)
            {
                _form.State = State.Expression;
                OpenBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.OpenBracket)
            {
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Expression)
            {
                _form.State = State.CloseBracket;
                Expression = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.Expression)
            {
                _form.State = State.CloseBracket;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedToken(GDCloseBracket token)
        {
            if (_form.State == State.CloseBracket)
            {
                _form.State = State.Completed;
                CloseBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.CloseBracket)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}