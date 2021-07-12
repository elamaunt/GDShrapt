namespace GDShrapt.Reader
{
    public sealed class GDBracketExpression : GDExpression, 
        ITokenOrSkipReceiver<GDOpenBracket>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDCloseBracket>,
        ITokenReceiver<GDNewLine>,
        INewLineReceiver
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Brackets);

        public GDOpenBracket OpenBracket
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDExpression InnerExpression
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDCloseBracket CloseBracket
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        enum State
        {
            OpenBracket,
            Expression,
            CloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDOpenBracket, GDExpression, GDCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public GDBracketExpression()
        {
            _form = new GDTokensForm<State, GDOpenBracket, GDExpression, GDCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.OpenBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveOpenBracket(c, state);
                    break;
                case State.Expression:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveExpression(c, state);
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
            switch (_form.State)
            {
                case State.OpenBracket:
                    state.PopAndPassNewLine();
                    break;
                case State.Expression:
                case State.CloseBracket:
                    _form.AddBeforeActiveToken(new GDNewLine());
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDBracketExpression();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedToken(GDOpenBracket token)
        {
            if (_form.IsOrLowerState(State.OpenBracket))
            {
                _form.State = State.Expression;
                OpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.OpenBracket))
            {
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.CloseBracket;
                InnerExpression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Expression))
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
            if (_form.State != State.Completed)
            {
                _form.AddBeforeActiveToken(token);
                return;
            }

            throw new GDInvalidStateException();
        }

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            if (_form.State != State.Completed)
            {
                _form.AddBeforeActiveToken(token);
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}