namespace GDShrapt.Reader
{
    public sealed class GDSingleOperatorExpression : GDExpression,
        ITokenOrSkipReceiver<GDSingleOperator>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenReceiver<GDNewLine>,
        INewLineReceiver
    {
        public override int Priority => GDHelper.GetOperatorPriority(OperatorType);

        public GDSingleOperator Operator
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDExpression TargetExpression
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDSingleOperatorType OperatorType { get => _form.Token0 == null ? GDSingleOperatorType.Null : _form.Token0.OperatorType; }

        public enum State
        {
            Operator,
            TargetExpression,
            Completed
        }

        readonly GDTokensForm<State, GDSingleOperator, GDExpression> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDSingleOperator, GDExpression> TypedForm => _form;
        public GDSingleOperatorExpression()
        {
            _form = new GDTokensForm<State, GDSingleOperator, GDExpression>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Operator:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveSingleOperator(c, state);
                    break;
                case State.TargetExpression:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveExpression(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_form.State == State.Completed)
                state.PopAndPassNewLine();
            else
                this.AddNewLine();
        }

        protected override GDExpression PriorityRebuildingPass()
        {
            if (IsHigherPriorityThan(TargetExpression, GDSideType.Left))
            {
                var previous = TargetExpression;
                TargetExpression = previous.SwapLeft(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            // TODO: may lose the data about syntax
            // Remove 'negate' operator for number expression. Just make the number negative.
            if (OperatorType == GDSingleOperatorType.Negate && TargetExpression is GDNumberExpression numberExpression)
            {
                numberExpression.Number.Negate();
                return numberExpression;
            }

            return this;
        }

        public override GDExpression SwapLeft(GDExpression expression)
        {
            var right = TargetExpression;
            TargetExpression = expression;
            return right;
        }

        public override GDExpression SwapRight(GDExpression expression)
        {
            var right = TargetExpression;
            TargetExpression = expression;
            return right;
        }

        public override void RebuildBranchesOfPriorityIfNeeded()
        {
            TargetExpression = TargetExpression.RebuildRootOfPriorityIfNeeded();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDSingleOperatorExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDSingleOperator>.HandleReceivedToken(GDSingleOperator token)
        {
            if (_form.IsOrLowerState(State.Operator))
            {
                _form.State = State.TargetExpression;
                Operator = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSingleOperator>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Operator))
            {
                _form.State = State.TargetExpression;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.TargetExpression))
            {
                _form.State = State.Completed;
                TargetExpression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.TargetExpression))
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

