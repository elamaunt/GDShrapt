namespace GDShrapt.Reader
{
    public sealed class GDStringExpression : GDExpression, ITokenOrSkipReceiver<GDStringNode>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);
        public GDStringNode String
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            String,
            Completed
        }

        readonly GDTokensForm<State, GDStringNode> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDStringNode> TypedForm => _form;
        public GDStringExpression()
        {
            _form = new GDTokensForm<State, GDStringNode>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.IsOrLowerState(State.String))
            {
                if (this.ResolveSpaceToken(c, state))
                    return;

                if (IsStringStartChar(c))
                {
                    this.ResolveString(c, state);
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

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDStringNode>.HandleReceivedToken(GDStringNode token)
        {
            if (_form.State == State.String)
            {
                String = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDStringNode>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.String)
            {
                _form.State = State.Completed;
                return;
            }
        
            throw new GDInvalidStateException();
        }
    }
}