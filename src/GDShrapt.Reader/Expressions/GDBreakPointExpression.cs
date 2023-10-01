namespace GDShrapt.Reader
{
    public sealed class GDBreakPointExpression : GDExpression,
        ITokenOrSkipReceiver<GDBreakPointKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Breakpoint);

        public GDBreakPointKeyword BreakPointKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            BreakPoint,
            Completed
        }

        readonly GDTokensForm<State, GDBreakPointKeyword> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDBreakPointKeyword> TypedForm => _form;
        public GDBreakPointExpression()
        {
            _form = new GDTokensForm<State, GDBreakPointKeyword>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.IsOrLowerState(State.BreakPoint))
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

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDBreakPointKeyword>.HandleReceivedToken(GDBreakPointKeyword token)
        {
            if (_form.IsOrLowerState(State.BreakPoint))
            {
                _form.State = State.Completed;
                BreakPointKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDBreakPointKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.BreakPoint))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
