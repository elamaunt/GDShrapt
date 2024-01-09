namespace GDShrapt.Reader
{
    public sealed class GDGetUniqueNodeExpression : GDExpression,
        ITokenOrSkipReceiver<GDPercent>,
        ITokenOrSkipReceiver<GDPathList>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.GetNode);

        public GDPercent Percent
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
            Percent,
            Path,
            Completed
        }

        readonly GDTokensForm<State, GDPercent, GDPathList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDPercent, GDPathList> TypedForm => _form;
        public GDGetUniqueNodeExpression()
        {
            _form = new GDTokensForm<State, GDPercent, GDPathList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Percent:
                    if (this.ResolveSpaceToken(c, state))
                        return;
                    this.ResolvePercent(c, state);
                    break;
                case State.Path:
                    _form.State = State.Completed;

                    if (this.ResolveSpaceToken(c, state))
                        return;
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

        void ITokenReceiver<GDPercent>.HandleReceivedToken(GDPercent token)
        {
            if (_form.IsOrLowerState(State.Percent))
            {
                _form.State = State.Path;
                Percent = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDPercent>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Percent))
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
