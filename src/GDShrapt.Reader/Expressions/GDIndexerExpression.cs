namespace GDShrapt.Reader
{
    public sealed class GDIndexerExpression : GDExpression,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDSquareOpenBracket>,
        ITokenOrSkipReceiver<GDSquareCloseBracket>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Indexer);

        public GDExpression CallerExpression
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDSquareOpenBracket SquareOpenBracket
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExpression InnerExpression
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDSquareCloseBracket SquareCloseBracket
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public enum State
        {
            Caller,
            SquareOpenBracket,
            Inner,
            SquareCloseBracket,
            Completed
        }


        readonly GDTokensForm<State, GDExpression, GDSquareOpenBracket, GDExpression, GDSquareCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDExpression, GDSquareOpenBracket, GDExpression, GDSquareCloseBracket> TypedForm => _form;
        public GDIndexerExpression()
        {
            _form = new GDTokensForm<State, GDExpression, GDSquareOpenBracket, GDExpression, GDSquareCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Caller:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveExpression(c, state);
                    break;
                case State.SquareOpenBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveSquareOpenBracket(c, state);
                    break;
                case State.Inner:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveExpression(c, state);
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
            state.PopAndPassNewLine();
        }

        protected override GDExpression PriorityRebuildingPass()
        {
            if (IsHigherPriorityThan(CallerExpression, GDSideType.Left))
            {
                var previous = CallerExpression;
                CallerExpression = previous.SwapRight(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            return this;
        }

        public override GDExpression SwapLeft(GDExpression expression)
        {
            var left = CallerExpression;
            CallerExpression = expression;
            return left;
        }

        public override GDExpression SwapRight(GDExpression expression)
        {
            var right = CallerExpression;
            CallerExpression = expression;
            return right;
        }

        public override void RebuildBranchesOfPriorityIfNeeded()
        {
            CallerExpression = CallerExpression.RebuildRootOfPriorityIfNeeded();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDIndexerExpression();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Caller))
            {
                _form.State = State.SquareOpenBracket;
                CallerExpression = token;
                return;
            }

            if (_form.IsOrLowerState(State.Inner))
            {
                _form.State = State.SquareCloseBracket;
                InnerExpression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Caller))
            {
                _form.State = State.SquareOpenBracket;
                return;
            }

            if (_form.IsOrLowerState(State.Inner))
            {
                _form.State = State.SquareCloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDSquareOpenBracket>.HandleReceivedToken(GDSquareOpenBracket token)
        {
            if (_form.IsOrLowerState(State.SquareOpenBracket))
            {
                _form.State = State.Inner;
                SquareOpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSquareOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.SquareOpenBracket))
            {
                _form.State = State.Inner;
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
    }
}
