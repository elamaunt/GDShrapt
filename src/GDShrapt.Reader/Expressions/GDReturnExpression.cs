namespace GDShrapt.Reader
{
    public sealed class GDReturnExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Return);

        public GDExpression ResultExpression { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (ResultExpression == null)
            {
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

        public override string ToString()
        {
            if (ResultExpression == null)
                return $"return";
            return $"return {ResultExpression}";
        }
    }
}
