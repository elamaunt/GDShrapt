namespace GDShrapt.Reader
{
    public class GDIfExpression : GDExpression
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
                return;

            if (!_trueChecked && TrueExpression == null)
            {
                _trueChecked = true;
                state.PushNode(new GDExpressionResolver(expr => TrueExpression = expr));
                state.PassChar(c);
                return;
            }

            if (!_conditionChecked && Condition == null)
            {
                _conditionChecked = true;
                state.PushNode(new GDExpressionResolver(expr => Condition = expr));
                state.PassChar(c);
                return;
            }

            if (!_elseKeyChecked)
            {
                _elseKeyChecked = true;
                state.PushNode(new GDStaticKeywordResolver("else ", result => _hasElse = result));
                state.PassChar(c);
                return;
            }

            if (_hasElse && !_falseChecked && FalseExpression == null)
            {
                _falseChecked = true;
                state.PushNode(new GDExpressionResolver(expr => FalseExpression = expr));
                state.PassChar(c);
                return;
            }

            state.PopNode();
            state.PassChar(c);

        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        /// <summary>
        /// Rebuilds current node if another inner node has higher priority.
        /// </summary>
        /// <returns>Same node if nothing changed or a new node which now the root</returns>
        protected override GDExpression PriorityRebuildingPass()
        {
            if (IsLowerPriorityThan(TrueExpression, GDSideType.Left))
            {
                var previous = TrueExpression;
                TrueExpression = TrueExpression.SwapRight(this).RebuildOfPriorityIfNeeded();
                return previous;
            }

            if (IsLowerPriorityThan(FalseExpression, GDSideType.Right))
            {
                var previous = FalseExpression;
                FalseExpression = FalseExpression.SwapLeft(this).RebuildOfPriorityIfNeeded();
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

        public override string ToString()
        {
            return $"{TrueExpression} if {Condition} else {FalseExpression}";
        }
    }
}
