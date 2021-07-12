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

        readonly GDTokensForm<State, GDString> _form;
        public override GDTokensForm Form => _form;
        public GDStringExpression()
        {
            _form = new GDTokensForm<State, GDString>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.IsOrLowerState(State.String))
            {
                if (this.ResolveSpaceToken(c, state))
                    return;

                if (IsStringStartChar(c))
                {
                    _form.State = State.Completed;
                    state.PushAndPass(String = new GDString(), c);
                }
                else
                {
                    _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(x => IsStringStartChar(x) || x == '\n')));
                    state.PassChar(c);
                }
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
            return new GDStringExpression();
        }
    }
}