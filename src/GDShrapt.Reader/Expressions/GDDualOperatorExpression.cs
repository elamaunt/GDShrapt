namespace GDShrapt.Reader
{
    public sealed class GDDualOperatorExpression : GDExpression,
        ITokenOrSkipReceiver<GDExpression>, 
        ITokenOrSkipReceiver<GDDualOperator>,
        ITokenReceiver<GDNewLine>,
        ITokenReceiver<GDLeftSlash>,
        INewLineReceiver,
        ILeftSlashReceiver
    {
        private bool _expectingNewLineTokenForSplitting;

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

        public enum State
        {
            LeftExpression,
            DualOperator,
            RightExpression,
            Completed
        }

        readonly GDTokensForm<State, GDExpression, GDDualOperator, GDExpression> _form;
        public bool AllowNewLines { get; }
        private bool MayHandleNewLine => (AllowNewLines && !_expectingNewLineTokenForSplitting) || (!AllowNewLines && _expectingNewLineTokenForSplitting);

        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDExpression, GDDualOperator, GDExpression> TypedForm => _form;
        public GDDualOperatorExpression()
            : this(true)
        {
        }

        public GDDualOperatorExpression(bool allowNewLines)
        {
            _form = new GDTokensForm<State, GDExpression, GDDualOperator, GDExpression>(this);
            AllowNewLines = allowNewLines;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveLeftSlashToken(c, state))
            {
                _expectingNewLineTokenForSplitting = true;
                return;
            }

            switch (_form.State)
            {
                case State.LeftExpression:
                    if (!this.ResolveSpaceToken(c, state))
                    {
                        _expectingNewLineTokenForSplitting = false;
                        this.ResolveExpression(c, state);
                    }
                    break;
                case State.DualOperator:
                    // Indicates that it isn't a normal expression. The parent should handle the state.
                    if (LeftExpression == null)
                    {
                        state.Pop();
                        state.PassChar(c);
                        return;
                    }

                    if (!this.ResolveSpaceToken(c, state))
                    {
                        _expectingNewLineTokenForSplitting = false;
                        this.ResolveDualOperator(c, state);
                    }
                    break;
                case State.RightExpression:
                    if (!this.ResolveSpaceToken(c, state))
                    {
                        _expectingNewLineTokenForSplitting = false;
                        this.ResolveExpression(c, state);
                    }
                    break;
                default:
                    _expectingNewLineTokenForSplitting = false;
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (MayHandleNewLine && _form.State != State.Completed)
            {
                _expectingNewLineTokenForSplitting = false;
                _form.AddBeforeActiveToken(new GDNewLine());
            }
            else
                state.PopAndPassNewLine();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            if (MayHandleNewLine && _form.State != State.Completed)
            {
                _form.AddBeforeActiveToken(state.Push(new GDComment()));
            }
            else
            {
                state.Pop();
            }

            state.PassSharpChar();
        }

        /// <summary>
        /// Rebuilds current node if another inner node has higher priority.
        /// </summary>
        /// <returns>Same node if nothing changed or a new node which now the root</returns>
        protected override GDExpression PriorityRebuildingPass()
        {
            if (LeftExpression != null && IsHigherPriorityThan(LeftExpression, GDSideType.Left))
            {
                var previous = LeftExpression;
                // Remove expression to break the cycle
                LeftExpression = null;
                LeftExpression = previous.SwapRight(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            if (RightExpression != null && IsHigherPriorityThan(RightExpression, GDSideType.Right))
            {
                var previous = RightExpression;
                // Remove expression to break the cycle
                RightExpression = null;
                RightExpression = previous.SwapLeft(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            return this;
        }

        public override void RebuildBranchesOfPriorityIfNeeded()
        {
            if (LeftExpression != null)
                LeftExpression = LeftExpression.RebuildRootOfPriorityIfNeeded();

            if (RightExpression != null)
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
            return new GDDualOperatorExpression(AllowNewLines);
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
            if (_form.IsOrLowerState(State.LeftExpression))
            {
                LeftExpression = token;
                _form.State = State.DualOperator;
                return;
            }

            if (_form.IsOrLowerState(State.RightExpression))
            {
                RightExpression = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.LeftExpression))
            {
                _form.State = State.DualOperator;
                return;
            }

            if (_form.IsOrLowerState(State.RightExpression))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDDualOperator>.HandleReceivedToken(GDDualOperator token)
        {
            if (_form.IsOrLowerState(State.DualOperator))
            {
                Operator = token;
                _form.State = State.RightExpression;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDDualOperator>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.DualOperator))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDNewLine>.HandleReceivedToken(GDNewLine token)
        {
            if (MayHandleNewLine && _form.State != State.Completed)
            {
                _expectingNewLineTokenForSplitting = false;
                _form.AddBeforeActiveToken(token);
                return;
            }

            throw new GDInvalidStateException();
        }

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            if (MayHandleNewLine && _form.State != State.Completed)
            {
                _expectingNewLineTokenForSplitting = false;
                _form.AddBeforeActiveToken(token);
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDLeftSlash>.HandleReceivedToken(GDLeftSlash token)
        {
            if (!AllowNewLines && _form.State != State.Completed)
            {
                _expectingNewLineTokenForSplitting = true;
                _form.AddBeforeActiveToken(token);
                return;
            }

            throw new GDInvalidStateException();
        }

        void ILeftSlashReceiver.HandleReceivedToken(GDLeftSlash token)
        {
            if (!AllowNewLines && _form.State != State.Completed)
            {
                _expectingNewLineTokenForSplitting = true;
                _form.AddBeforeActiveToken(token);
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}