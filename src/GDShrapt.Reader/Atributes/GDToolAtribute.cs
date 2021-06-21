namespace GDShrapt.Reader
{
    public class GDToolAtribute : GDClassAtribute, IKeywordReceiver<GDToolKeyword>
    {
        internal GDToolKeyword ToolKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            Tool,
            Completed
        }

        readonly GDTokensForm<State, GDToolKeyword> _form;
        internal override GDTokensForm Form => _form;
        public GDToolAtribute()
        {
            _form = new GDTokensForm<State, GDToolKeyword>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            if (_form.State == State.Tool)
            {
                state.PushAndPass(new GDKeywordResolver<GDToolKeyword>(this), c);
                return;
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        void IKeywordReceiver<GDToolKeyword>.HandleReceivedToken(GDToolKeyword token)
        {
            if (_form.State == State.Tool)
            {
                _form.State = State.Completed;
                ToolKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDToolKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Tool)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}