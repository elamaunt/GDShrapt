namespace GDShrapt.Reader
{
    public sealed class GDGetNodeExpression : GDExpression,
        ITokenReceiver<GDDollar>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.GetNode);

        public GDDollar Dollar
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDPathList Path 
        {
            get => _form.Token1 ?? (_form.Token1 = new GDPathList());
            set => _form.Token1 = value;
        }

        enum State
        {
            Dollar,
            Path,
            Completed
        }

        readonly GDTokensForm<State, GDDollar, GDPathList> _form;
        public override GDTokensForm Form => _form;
        public GDGetNodeExpression()
        {
            _form = new GDTokensForm<State, GDDollar, GDPathList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Dollar:
                    if (this.ResolveStyleToken(c, state))
                        return;
                    this.ResolveDollar(c, state);
                    break;
                case State.Path:
                    _form.State = State.Completed;
                    state.PushAndPass(Path, c);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDGetNodeExpression();
        }

        void ITokenReceiver<GDDollar>.HandleReceivedToken(GDDollar token)
        {
            if (_form.State == State.Dollar)
            {
                _form.State = State.Path;
                Dollar = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDDollar>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Dollar)
            {
                _form.State = State.Path;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
