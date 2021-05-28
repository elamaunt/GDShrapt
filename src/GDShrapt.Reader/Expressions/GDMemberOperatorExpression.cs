namespace GDShrapt.Reader
{
    public class GDMemberOperatorExpression : GDExpression
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
                state.PushNode(new GDExpressionResolver(expr => CallerExpression = expr));
                state.HandleChar(c);
                return;
            }

            if (Identifier == null)
            {
                state.PushNode(Identifier = new GDIdentifier());

                if (c != '.')
                    state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }

        /* public override GDExpression CombineLeft(GDExpression expr)
         {
             CallerExpression = expr;
             return base.CombineLeft(expr);
         }*/

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

        public override string ToString()
        {
            return $"{CallerExpression}.{Identifier}";
        }
    }
}