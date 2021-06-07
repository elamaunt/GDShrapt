namespace GDShrapt.Reader
{
    public sealed class GDBracketExpression : GDExpression
    {
        bool _expressionChecked;

        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Brackets);

        public GDExpression InnerExpression { get; set; }
        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (!_expressionChecked && InnerExpression == null)
            {
                _expressionChecked = true;
                state.PushNode(new GDExpressionResolver(expr => InnerExpression = expr));
                state.PassChar(c);
                return;
            }

            state.PopNode();

            if (c != ')')
                state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"({InnerExpression})";
        }
    }
}