namespace GDShrapt.Reader
{
    public sealed class GDIdentifierExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Identifier);
        public GDIdentifier Identifier
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            Identifier,
            Completed
        }

        readonly GDTokensForm<State, GDIdentifier> _form = new GDTokensForm<State, GDIdentifier>();
        internal override GDTokensForm Form => _form;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            if (_form.State == State.Identifier)
            {
                _form.State = State.Completed;
                state.Push(Identifier = new GDIdentifier());
                state.PassChar(c);
                return;
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }
    }
}