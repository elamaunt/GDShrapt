namespace GDShrapt.Reader
{
    public class GDReturnExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Return);

        public GDExpression ResultExpression { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (ResultExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => ResultExpression = expr));
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

        public override string ToString()
        {
            if (ResultExpression == null)
                return $"return";
            return $"return {ResultExpression}";
        }
    }
}
