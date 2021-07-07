namespace GDShrapt.Reader
{
    public sealed class GDDualOperatorExpression : GDExpression, IExpressionsReceiver, IDualOperatorReceiver
    {
        public override int Priority => GDHelper.GetOperatorPriority(OperatorType);
        public override GDAssociationOrderType AssociationOrder => GDHelper.GetOperatorAssociationOrder(OperatorType);

        public GDExpression LeftExpression 
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDDualOperator Operator
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExpression RightExpression
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public GDDualOperatorType OperatorType
        {
            get => _form.Token1 == null ? GDDualOperatorType.Null : _form.Token1.OperatorType;
        }

        enum State
        {
            LeftExpression,
            DualOperator,
            RightExpression,
            Completed
        }

        readonly GDTokensForm<State, GDExpression, GDDualOperator, GDExpression> _form;
        public override GDTokensForm Form => _form;
        public GDDualOperatorExpression()
        {
            _form = new GDTokensForm<State, GDExpression, GDDualOperator, GDExpression>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.LeftExpression:
                    if (!this.ResolveStyleToken(c, state))
                        state.PushAndPass(new GDExpressionResolver(this), c);
                    break;
                case State.DualOperator:
                    // Indicates that it isn't a normal expression. The parent should handle the state.
                    if (LeftExpression == null)
                    {
                        state.Pop();
                        state.PassChar(c);
                        return;
                    }

                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveDualOperator(c, state);
                    break;
                case State.RightExpression:
                    if (!this.ResolveStyleToken(c, state))
                        state.PushAndPass(new GDExpressionResolver(this), c);
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
                case State.LeftExpression:
                case State.DualOperator:
                case State.RightExpression:
                    _form.AddBeforeActiveToken(state.Push(new GDNewLine()));
                    break;
                case State.Completed:
                    state.Pop();
                    state.PassNewLine();
                    break;
                default:
                    break;
            }
            
        }


        /// <summary>
        /// Rebuilds current node if another inner node has higher priority.
        /// </summary>
        /// <returns>Same node if nothing changed or a new node which now the root</returns>
        protected override GDExpression PriorityRebuildingPass()
        {
            if (IsHigherPriorityThan(LeftExpression, GDSideType.Left))
            {
                var previous = LeftExpression;
                // Remove expression to break cycle
                LeftExpression = null;
                LeftExpression = previous.SwapRight(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            if (IsHigherPriorityThan(RightExpression, GDSideType.Right))
            {
                var previous = RightExpression;
                // Remove expression to break cycle
                RightExpression = null;
                RightExpression = previous.SwapLeft(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            return this;
        }

        public override void RebuildBranchesOfPriorityIfNeeded()
        {
            LeftExpression = LeftExpression.RebuildRootOfPriorityIfNeeded();
            RightExpression = RightExpression.RebuildRootOfPriorityIfNeeded();
        }

        public override GDExpression SwapLeft(GDExpression expression)
        {
            var left = LeftExpression;
            LeftExpression = expression;
            return left;
        }

        public override GDExpression SwapRight(GDExpression expression)
        {
            var right = RightExpression;
            RightExpression = expression;
            return right;
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDDualOperatorExpression();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.LeftExpression)
            {
                LeftExpression = token;
                _form.State = State.DualOperator;
                return;
            }

            if (_form.State == State.RightExpression)
            {
                RightExpression = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.LeftExpression)
            {
                _form.State = State.DualOperator;
                return;
            }

            if (_form.State == State.RightExpression)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IDualOperatorReceiver.HandleReceivedToken(GDDualOperator token)
        {
            if (_form.State == State.DualOperator)
            {
                Operator = token;
                _form.State = State.RightExpression;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IDualOperatorReceiver.HandleDualOperatorSkip()
        {
            if (_form.State == State.DualOperator)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}