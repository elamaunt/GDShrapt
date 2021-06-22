namespace GDShrapt.Reader
{
    public sealed class GDMemberOperatorExpression : GDExpression,
        IExpressionsReceiver,
        ITokenReceiver<GDPoint>,
        IIdentifierReceiver
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Member);

        public GDExpression CallerExpression
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        internal GDPoint Point
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDIdentifier Identifier
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        enum State
        {
            CallerExpression,
            Point,
            Identifier,
            Completed
        }

        readonly GDTokensForm<State, GDExpression, GDPoint, GDIdentifier> _form;
        internal override GDTokensForm Form => _form;
        public GDMemberOperatorExpression()
        {
            _form = new GDTokensForm<State, GDExpression, GDPoint, GDIdentifier>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.CallerExpression:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveExpression(c, state);
                    break;
                case State.Point:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolvePoint(c, state);
                    break;
                case State.Identifier:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveIdentifier(c, state);
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

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.CallerExpression)
            {
                _form.State = State.Point;
                CallerExpression = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.CallerExpression)
            {
                _form.State = State.Point;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDPoint>.HandleReceivedToken(GDPoint token)
        {
            if (_form.StateIndex <= (int)State.Point)
            {
                _form.State = State.Identifier;
                Point = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDPoint>.HandleReceivedTokenSkip()
        {
            if (_form.StateIndex <= (int)State.Point)
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Completed;
                Identifier = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedIdentifierSkip()
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}