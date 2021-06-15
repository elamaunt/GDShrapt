namespace GDShrapt.Reader
{
    public sealed class GDIfExpression : GDExpression,
        IExpressionsReceiver,
        IKeywordReceiver<GDIfKeyword>,
        IKeywordReceiver<GDElseKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.If);

        public GDExpression TrueExpression
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        internal GDIfKeyword IfKeyword
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExpression Condition
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        internal GDElseKeyword ElseKeyword
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public GDExpression FalseExpression
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        enum State
        {
            True,
            If,
            Condition,
            Else,
            False,
            Completed
        }

        readonly GDTokensForm<State, GDExpression, GDIfKeyword, GDExpression, GDElseKeyword, GDExpression> _form = new GDTokensForm<State, GDExpression, GDIfKeyword, GDExpression, GDElseKeyword, GDExpression>();
        internal override GDTokensForm Form => _form;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.True:
                    state.Push(new GDExpressionResolver(this));
                    state.PassChar(c);
                    break;
                case State.If:
                    state.Push(new GDKeywordResolver<GDIfKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Condition:
                    state.Push(new GDExpressionResolver(this));
                    state.PassChar(c);
                    break;
                case State.Else:
                    state.Push(new GDKeywordResolver<GDElseKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.False:
                    state.Push(new GDExpressionResolver(this));
                    state.PassChar(c);
                    break;
                default:
                    state.Pop();
                    state.PassChar(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        protected override GDExpression PriorityRebuildingPass()
        {
            if (IsHigherPriorityThan(TrueExpression, GDSideType.Left))
            {
                var previous = TrueExpression;
                // Remove expression to break cycle
                TrueExpression = null;
                TrueExpression = previous.SwapRight(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            if (IsHigherPriorityThan(FalseExpression, GDSideType.Right))
            {
                var previous = FalseExpression;

                // Remove expression to break cycle
                FalseExpression = null;
                FalseExpression = previous.SwapLeft(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            return this;
        }

        public override GDExpression SwapLeft(GDExpression expression)
        {
            var left = TrueExpression;
            TrueExpression = expression;
            return left;
        }

        public override GDExpression SwapRight(GDExpression expression)
        {
            var right = FalseExpression;
            FalseExpression = expression;
            return right;
        }

        public override void RebuildBranchesOfPriorityIfNeeded()
        {
            TrueExpression = TrueExpression.RebuildRootOfPriorityIfNeeded();
            FalseExpression = FalseExpression.RebuildRootOfPriorityIfNeeded();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.True)
            {
                _form.State = State.If;
                TrueExpression = token;
                return;
            }

            if (_form.State == State.Condition)
            {
                _form.State = State.Else;
                Condition = token;
                return;
            }

            if (_form.State == State.False)
            {
                _form.State = State.Completed;
                FalseExpression = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.True)
            {
                _form.State = State.If;
                return;
            }

            if (_form.State == State.Condition)
            {
                _form.State = State.Else;
                return;
            }

            if (_form.State == State.False)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDIfKeyword>.HandleReceivedToken(GDIfKeyword token)
        {
            if (_form.State == State.If)
            {
                _form.State = State.Condition;
                IfKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDIfKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.If)
            {
                _form.State = State.Condition;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDElseKeyword>.HandleReceivedToken(GDElseKeyword token)
        {
            if (_form.State == State.Else)
            {
                _form.State = State.False;
                ElseKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDElseKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Else)
            {
                _form.State = State.False;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
