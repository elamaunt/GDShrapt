namespace GDShrapt.Reader
{
    public sealed class GDMemberOperatorExpression : GDExpression, IExpressionsReceiver
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Member);

        public GDExpression CallerExpression { get; set; }
        public GDIdentifier Identifier { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (CallerExpression == null)
            {
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }

            if (Identifier == null)
            {
                state.Push(Identifier = new GDIdentifier());

                if (c != '.')
                    state.PassChar(c);
                return;
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
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

        public override string ToString()
        {
            return $"{CallerExpression}.{Identifier}";
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            throw new System.NotImplementedException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDComment token)
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDSpace token)
        {
            throw new System.NotImplementedException();
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}