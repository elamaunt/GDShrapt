namespace GDShrapt.Reader
{
    public sealed class GDBreakPointExpression : GDExpression,
        IKeywordReceiver<GDBreakPointKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Breakpoint);

        public GDBreakPointKeyword BreakPointKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            BreakPoint,
            Completed
        }

        readonly GDTokensForm<State, GDBreakPointKeyword> _form;
        public override GDTokensForm Form => _form;
        public GDBreakPointExpression()
        {
            _form = new GDTokensForm<State, GDBreakPointKeyword>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.BreakPoint)
                state.Push(new GDKeywordResolver<GDBreakPointKeyword>(this));
            else
                state.Pop();

            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDBreakPointExpression();
        }

        void IKeywordReceiver<GDBreakPointKeyword>.HandleReceivedToken(GDBreakPointKeyword token)
        {
            if (_form.State == State.BreakPoint)
            {
                _form.State = State.Completed;
                BreakPointKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDBreakPointKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.BreakPoint)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
