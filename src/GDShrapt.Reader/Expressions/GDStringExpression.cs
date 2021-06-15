namespace GDShrapt.Reader
{
    public sealed class GDStringExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);
        public GDString String
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            String,
            Completed
        }

        readonly GDTokensForm<State, GDString> _form = new GDTokensForm<State, GDString>();

        internal override GDTokensForm Form => _form;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.String)
            {
                if (IsStringStartChar(c))
                {
                    _form.State = State.Completed;
                    state.Push(String = new GDString());
                    state.PassChar(c);
                }
                else
                {
                    _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(x => IsStringStartChar(x) || x == '\n')));
                    state.PassChar(c);
                }
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