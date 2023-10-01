namespace GDShrapt.Reader
{
    public sealed class GDArrayInitializerExpression : GDExpression,
        ITokenOrSkipReceiver<GDSquareOpenBracket>,
        ITokenOrSkipReceiver<GDExpressionsList>,
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

        public enum State
        {
            SquareOpenBracket,
            Values,
            SquareCloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDSquareOpenBracket, GDExpressionsList, GDSquareCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDSquareOpenBracket, GDExpressionsList, GDSquareCloseBracket> TypedForm => _form;
        public GDArrayInitializerExpression()
        {
            _form = new GDTokensForm<State, GDSquareOpenBracket, GDExpressionsList, GDSquareCloseBracket>(this);
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
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
            if (_form.IsOrLowerState(State.Values))
            {
                _form.State = State.SquareCloseBracket;
                state.PushAndPassNewLine(Values);
                return;
            }

            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDArrayInitializerExpression();
        }

        void ITokenReceiver<GDSquareOpenBracket>.HandleReceivedToken(GDSquareOpenBracket token)
        {
            if (_form.IsOrLowerState(State.SquareOpenBracket))
            {
                _form.State = State.Values;
                SquareOpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSquareOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.SquareOpenBracket))
            {
                _form.State = State.Values;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDSquareCloseBracket>.HandleReceivedToken(GDSquareCloseBracket token)
        {
            if (_form.IsOrLowerState(State.SquareCloseBracket))
            {
                _form.State = State.Completed;
                SquareCloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSquareCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.SquareCloseBracket))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpressionsList>.HandleReceivedToken(GDExpressionsList token)
        {
            if (_form.IsOrLowerState(State.Values))
            {
                Values = token;
                _form.State = State.SquareCloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpressionsList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Values))
            {
                _form.State = State.SquareCloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
