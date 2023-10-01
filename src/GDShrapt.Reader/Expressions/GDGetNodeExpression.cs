namespace GDShrapt.Reader
{
    public sealed class GDGetNodeExpression : GDExpression,
        ITokenOrSkipReceiver<GDDollar>,
        ITokenOrSkipReceiver<GDPathList>
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

        public enum State
        {
            Dollar,
            Path,
            Completed
        }

        readonly GDTokensForm<State, GDDollar, GDPathList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDDollar, GDPathList> TypedForm => _form;
        public GDGetNodeExpression()
        {
            _form = new GDTokensForm<State, GDDollar, GDPathList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Dollar:
                    if (this.ResolveSpaceToken(c, state))
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

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDDollar>.HandleReceivedToken(GDDollar token)
        {
            if (_form.IsOrLowerState(State.Dollar))
            {
                _form.State = State.Path;
                Dollar = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDDollar>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Dollar))
            {
                _form.State = State.Path;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDPathList>.HandleReceivedToken(GDPathList token)
        {
            if (_form.IsOrLowerState(State.Path))
            {
                _form.State = State.Path;
                Path = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDPathList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Path))
            {
                _form.State = State.Path;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
