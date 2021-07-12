namespace GDShrapt.Reader
{
    public sealed class GDYieldExpression : GDExpression,
        ITokenOrSkipReceiver<GDYieldKeyword>,
        ITokenOrSkipReceiver<GDOpenBracket>,
        ITokenOrSkipReceiver<GDExpressionsList>,
        ITokenOrSkipReceiver<GDCloseBracket>,
        ITokenReceiver<GDNewLine>,
        INewLineReceiver
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Yield);

        public GDYieldKeyword YieldKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDOpenBracket OpenBracket
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExpressionsList Parameters
        {
            get => _form.Token2 ?? (_form.Token2 = new GDExpressionsList());
            set => _form.Token2 = value;
        }
        public GDCloseBracket CloseBracket
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        enum State
        {
            Yield,
            OpenBracket,
            Parameters,
            CloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDYieldKeyword, GDOpenBracket, GDExpressionsList, GDCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public GDYieldExpression()
        {
            _form = new GDTokensForm<State, GDYieldKeyword, GDOpenBracket, GDExpressionsList, GDCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Yield:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDYieldKeyword>(c, state);
                    break;
                case State.OpenBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveOpenBracket(c, state);
                    break;
                case State.Parameters:
                    _form.State = State.CloseBracket;
                    state.PushAndPass(Parameters, c);
                    break;
                case State.CloseBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveCloseBracket(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                state.PushAndPassNewLine(Parameters);
                return;
            }

            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDYieldExpression();
        }
        void ITokenReceiver<GDYieldKeyword>.HandleReceivedToken(GDYieldKeyword token)
        {
            if (_form.IsOrLowerState(State.Yield))
            {
                _form.State = State.OpenBracket;
                YieldKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDYieldKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Yield))
            {
                _form.State = State.OpenBracket;
                return;
            }

            throw new GDInvalidStateException();
        }
        void ITokenReceiver<GDOpenBracket>.HandleReceivedToken(GDOpenBracket token)
        {
            if (_form.IsOrLowerState(State.OpenBracket))
            {
                _form.State = State.Parameters;
                OpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.OpenBracket))
            {
                _form.State = State.Parameters;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpressionsList>.HandleReceivedToken(GDExpressionsList token)
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                Parameters = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpressionsList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedToken(GDCloseBracket token)
        {
            if (_form.IsOrLowerState(State.CloseBracket))
            {
                _form.State = State.Completed;
                CloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.CloseBracket))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDNewLine>.HandleReceivedToken(GDNewLine token)
        {
            if (_form.State == State.Parameters || _form.State == State.CloseBracket)
            {
                _form.AddBeforeActiveToken(token);
                return;
            }

            throw new GDInvalidStateException();
        }

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            if (_form.State == State.Parameters || _form.State == State.CloseBracket)
            {
                _form.AddBeforeActiveToken(token);
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}