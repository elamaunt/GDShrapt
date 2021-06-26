namespace GDShrapt.Reader
{
    public sealed class GDIndexerExression : GDExpression,
        IExpressionsReceiver,
        ITokenReceiver<GDSquareOpenBracket>,
        ITokenReceiver<GDSquareCloseBracket>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Indexer);

        public GDExpression CallerExpression
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        internal GDSquareOpenBracket SquareOpenBracket
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExpression InnerExpression
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        internal GDSquareCloseBracket SquareCloseBracket
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        enum State
        {
            Caller,
            SquareOpenBracket,
            Inner,
            SquareCloseBracket,
            Completed
        }


        readonly GDTokensForm<State, GDExpression, GDSquareOpenBracket, GDExpression, GDSquareCloseBracket> _form;
        internal override GDTokensForm Form => _form;
        public GDIndexerExression()
        {
            _form = new GDTokensForm<State, GDExpression, GDSquareOpenBracket, GDExpression, GDSquareCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Caller:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveExpression(c, state);
                    break;
                case State.SquareOpenBracket:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveSquareOpenBracket(c, state);
                    break;
                case State.Inner:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveExpression(c, state);
                    break;
                case State.SquareCloseBracket:
                    if (!this.ResolveStyleToken(c, state))
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
            return new GDIndexerExression();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Caller)
            {
                _form.State = State.SquareOpenBracket;
                CallerExpression = token;
                return;
            }

            if (_form.State == State.Inner)
            {
                _form.State = State.SquareCloseBracket;
                InnerExpression = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.Caller)
            {
                _form.State = State.SquareOpenBracket;
                return;
            }

            if (_form.State == State.Inner)
            {
                _form.State = State.SquareCloseBracket;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDSquareOpenBracket>.HandleReceivedToken(GDSquareOpenBracket token)
        {
            if (_form.State == State.SquareOpenBracket)
            {
                _form.State = State.Inner;
                SquareOpenBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDSquareOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.SquareOpenBracket)
            {
                _form.State = State.Inner;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDSquareCloseBracket>.HandleReceivedToken(GDSquareCloseBracket token)
        {
            if (_form.State == State.SquareCloseBracket)
            {
                _form.State = State.Completed;
                SquareCloseBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDSquareCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.SquareCloseBracket)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
