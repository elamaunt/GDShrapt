namespace GDSharp.Reader
{
    public class GDReturnStatement : GDStatement
    {
        public GDExpression ResultExpression { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (ResultExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => ResultExpression = expr));
                state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }

        public override string ToString()
        {
            if (ResultExpression == null)
                return $"return";
            return $"return {ResultExpression}";
        }
    }
}