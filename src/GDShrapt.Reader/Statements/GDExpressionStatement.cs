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
            return $"{Expression}";
        }
    }
}