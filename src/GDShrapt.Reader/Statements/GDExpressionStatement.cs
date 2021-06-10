namespace GDShrapt.Reader
{
    public sealed class GDExpressionStatement : GDStatement, IExpressionsReceiver
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

        public void HandleReceivedToken(GDExpression token)
        {
            throw new System.NotImplementedException();
        }

        public void HandleReceivedExpressionSkip()
        {
            throw new System.NotImplementedException();
        }

        public void HandleReceivedToken(GDComment token)
        {
            throw new System.NotImplementedException();
        }

        public void HandleReceivedToken(GDNewLine token)
        {
            throw new System.NotImplementedException();
        }

        public void HandleReceivedToken(GDSpace token)
        {
            throw new System.NotImplementedException();
        }

        public void HandleReceivedToken(GDInvalidToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}