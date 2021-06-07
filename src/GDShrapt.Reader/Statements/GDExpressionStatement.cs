namespace GDShrapt.Reader
{
    public sealed class GDExpressionStatement : GDStatement
    {
        public GDExpression Expression { get; set; }

        internal GDExpressionStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDExpressionStatement()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (Expression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => Expression = expr));
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
            return $"{Expression}";
        }
    }
}