namespace GDShrapt.Reader
{
    public class GDExtendsAtribute : GDClassAtribute
    {
        public GDType Type
        {
            get => (GDType)_form.Token0;
            set => _form.Token0 = value;
        }
        public GDString Path
        {
            get => (GDString)_form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            TypeOrPath,
            Completed
        }

        readonly GDTokensForm<State, GDSimpleSyntaxToken> _form = new GDTokensForm<State, GDSimpleSyntaxToken>();
        internal override GDTokensForm Form => _form;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            if (_form.State == State.TypeOrPath)
            {
                if (IsStringStartChar(c))
                {
                    _form.State = State.Completed;
                    state.Push(Path = new GDString());
                }
                else
                {
                    if (IsIdentifierStartChar(c))
                    {
                        _form.State = State.Completed;
                        state.Push(Type = new GDType());
                    }
                    else
                        state.Pop();
                }

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