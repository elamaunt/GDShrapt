namespace GDShrapt.Reader
{
    public sealed class GDAwaitExpression : GDExpression,
        ITokenOrSkipReceiver<GDAwaitKeyword>,
        ITokenOrSkipReceiver<GDOpenBracket>,
        ITokenOrSkipReceiver<GDExpressionsList>,
        ITokenOrSkipReceiver<GDCloseBracket>,
        ITokenReceiver<GDNewLine>,
        INewLineReceiver
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Await);

        public GDAwaitKeyword AwaitKeyword
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
            get => _form.Token2 ?? (_form.Token2 = new GDExpressionsList(_intendation));
            set => _form.Token2 = value;
        }
        public GDCloseBracket CloseBracket
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public enum State
        {
            Await,
            OpenBracket,
            Parameters,
            CloseBracket,
            Completed
        }

        readonly int _intendation;
        readonly GDTokensForm<State, GDAwaitKeyword, GDOpenBracket, GDExpressionsList, GDCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDAwaitKeyword, GDOpenBracket, GDExpressionsList, GDCloseBracket> TypedForm => _form;

        internal GDAwaitExpression(int intendation)
        {
            _intendation = intendation;
            _form = new GDTokensForm<State, GDAwaitKeyword, GDOpenBracket, GDExpressionsList, GDCloseBracket>(this);
        }

        public GDAwaitExpression()
        {
            _form = new GDTokensForm<State, GDAwaitKeyword, GDOpenBracket, GDExpressionsList, GDCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Await:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDAwaitKeyword>(c, state);
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

        internal override void HandleSharpChar(GDReadingState state)
        {
            if (_form.State == State.CloseBracket || _form.State == State.Parameters)
            {
                _form.AddBeforeActiveToken(state.Push(new GDComment()));
                state.PassSharpChar();
            }
            else
                base.HandleSharpChar(state);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDAwaitExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDAwaitKeyword>.HandleReceivedToken(GDAwaitKeyword token)
        {
            if (_form.IsOrLowerState(State.Await))
            {
                _form.State = State.OpenBracket;
                AwaitKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAwaitKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Await))
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