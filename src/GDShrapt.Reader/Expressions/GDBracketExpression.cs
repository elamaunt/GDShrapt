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
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }

            state.Pop();

            if (c != ')')
                state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"({InnerExpression})";
        }
    }
}