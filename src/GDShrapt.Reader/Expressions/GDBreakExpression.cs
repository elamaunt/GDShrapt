namespace GDShrapt.Reader
{
    public sealed class GDBreakExpression : GDExpression,
        IKeywordReceiver<GDBreakKeyword>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Break);

        internal GDBreakKeyword BreakKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        enum State
        {
            Break,
            Completed
        }

        readonly GDTokensForm<State, GDBreakKeyword> _form = new GDTokensForm<State, GDBreakKeyword>();
        internal override GDTokensForm Form => _form;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.Break)
                state.Push(new GDKeywordResolver<GDBreakKeyword>(this));
            else
                state.Pop();

            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        void IKeywordReceiver<GDBreakKeyword>.HandleReceivedToken(GDBreakKeyword token)
        {
            if (_form.State == State.Break)
            {
                _form.State = State.Completed;
                BreakKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDBreakKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Break)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
