namespace GDShrapt.Reader
{
    public sealed class GDIfExpression : GDExpression
    {
        bool _elseKeyChecked;
        bool _hasElse;

        bool _trueChecked;
        bool _conditionChecked;
        bool _falseChecked;

        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.If);

        public GDExpression TrueExpression { get; set; }
        public GDExpression Condition { get; set; }
        public GDExpression FalseExpression { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                SwitchTo(new GDSpace(), state);
                return;
            }

            if (!_trueChecked && TrueExpression == null)
            {
                _trueChecked = true;
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }

            if (!_conditionChecked && Condition == null)
            {
                _conditionChecked = true;
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }

            if (!_elseKeyChecked)
            {
                _elseKeyChecked = true;
                state.Push(new GDStaticKeywordResolver(this));
                state.PassChar(c);
                return;
            }

            if (_hasElse && !_falseChecked && FalseExpression == null)
            {
                _falseChecked = true;
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }

            state.Pop();
            state.PassChar(c);

        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        /// <summary>
        /// Rebuilds current node if another inner node has higher priority.
        /// </summary>
        /// <returns>Same node if nothing changed or a new node which now the root</returns>
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

        public override string ToString()
        {
            return $"{TrueExpression} if {Condition} else {FalseExpression}";
        }
    }
}
