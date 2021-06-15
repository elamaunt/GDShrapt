namespace GDShrapt.Reader
{
    public sealed class GDExpressionStatement : GDStatement, IExpressionsReceiver
    {
        public GDExpression Expression
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            Expression,
            Completed
        }

        readonly GDTokensForm<State, GDExpression> _form = new GDTokensForm<State, GDExpression>();
        internal override GDTokensForm Form => _form;

        internal GDExpressionStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDExpressionStatement()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.Expression)
                state.Push(new GDExpressionResolver(this));
            else
                state.Pop();

            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Expression)
            {
                _form.State = State.Completed;
                Expression = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.Expression)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}