namespace GDShrapt.Reader
{
    public sealed class GDArrayInitializerExpression : GDExpression,
        ITokenOrSkipReceiver<GDSquareOpenBracket>,
        ITokenOrSkipReceiver<GDSquareCloseBracket>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.ArrayInitializer);

        public GDSquareOpenBracket SquareOpenBracket
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDExpressionsList Values 
        {
            get => _form.Token1 ?? (_form.Token1 = new GDExpressionsList());
            set => _form.Token1 = value;
        }
        public GDSquareCloseBracket SquareCloseBracket
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        enum State
        {
            SquareOpenBracket,
            Values,
            SquareCloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDSquareOpenBracket, GDExpressionsList, GDSquareCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public GDArrayInitializerExpression()
        {
            _form = new GDTokensForm<State, GDSquareOpenBracket, GDExpressionsList, GDSquareCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.SquareOpenBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveSquareOpenBracket(c, state);
                    break;
                case State.Values:
                    _form.State = State.SquareCloseBracket;
                    state.PushAndPass(Values, c);
                    break;
                case State.SquareCloseBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveSquareCloseBracket(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_form.State == State.Values)
            {
                _form.State = State.SquareCloseBracket;
                state.PushAndPassNewLine(Values);
                return;
            }

            state.PopAndPassNewLine();
        }

        void ITokenReceiver<GDSquareOpenBracket>.HandleReceivedToken(GDSquareOpenBracket token)
        {
            if (_form.State == State.SquareOpenBracket)
            {
                _form.State = State.Values;
                SquareOpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSquareOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.SquareOpenBracket)
            {
                _form.State = State.Values;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDSquareCloseBracket>.HandleReceivedToken(GDSquareCloseBracket token)
        {
            if (_form.State == State.SquareCloseBracket)
            {
                _form.State = State.Completed;
                SquareCloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSquareCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.SquareCloseBracket)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDArrayInitializerExpression();
        }
    }
}
