namespace GDShrapt.Reader
{
    public sealed class GDCallExpression : GDExpression,
        ITokenOrSkipReceiver<GDOpenBracket>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDExpressionsList>,
        ITokenOrSkipReceiver<GDCloseBracket>,
        ITokenReceiver<GDNewLine>,
        INewLineReceiver
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Call);

        public GDExpression CallerExpression
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
            Caller,
            OpenBracket,
            Parameters,
            CloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDExpression, GDOpenBracket, GDExpressionsList, GDCloseBracket> _form;
        readonly int _intendation;

        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDExpression, GDOpenBracket, GDExpressionsList, GDCloseBracket> TypedForm => _form;

        internal GDCallExpression(int intendation)
        {
            _form = new GDTokensForm<State, GDExpression, GDOpenBracket, GDExpressionsList, GDCloseBracket>(this);
            _intendation = intendation;
        }

        public GDCallExpression()
        {
            _form = new GDTokensForm<State, GDExpression, GDOpenBracket, GDExpressionsList, GDCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Caller:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveExpression(c, state, _intendation);
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
            _form.AddBeforeActiveToken(state.Push(new GDComment()));
            state.PassSharpChar();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDCallExpression();
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
            if (_form.IsOrLowerState(State.Caller))
            {
                _form.State = State.OpenBracket;
                CallerExpression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Caller))
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