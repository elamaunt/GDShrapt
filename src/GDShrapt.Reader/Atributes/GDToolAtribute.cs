namespace GDShrapt.Reader
{
    public sealed class GDToolAtribute : GDClassAtribute, ITokenOrSkipReceiver<GDToolKeyword>
    {
        public GDToolKeyword ToolKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            Tool,
            Completed
        }

        readonly GDTokensForm<State, GDToolKeyword> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDToolKeyword> TypedForm => _form;
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

            if (_form.IsOrLowerState(State.Tool))
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

        public override GDNode CreateEmptyInstance()
        {
            return new GDToolAtribute();
        }

        void ITokenReceiver<GDToolKeyword>.HandleReceivedToken(GDToolKeyword token)
        {
            if (_form.IsOrLowerState(State.Tool))
            {
                _form.State = State.Completed;
                ToolKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }
        
        void ITokenSkipReceiver<GDToolKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Tool))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}