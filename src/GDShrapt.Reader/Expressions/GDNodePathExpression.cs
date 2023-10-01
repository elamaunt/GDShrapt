namespace GDShrapt.Reader
{
    public sealed class GDNodePathExpression : GDExpression,
        ITokenOrSkipReceiver<GDSky>,
        ITokenOrSkipReceiver<GDPathList>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.NodePath);

        public GDSky Sky
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
            Sky, 
            Path,
            Completed
        }

        readonly GDTokensForm<State, GDSky, GDPathList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDSky, GDPathList> TypedForm => _form;
        public GDNodePathExpression()
        {
            _form = new GDTokensForm<State, GDSky, GDPathList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Sky:
                    if (this.ResolveSpaceToken(c, state))
                        return;
                    this.ResolveSky(c, state);
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
            return new GDNodePathExpression();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDSky>.HandleReceivedToken(GDSky token)
        {
            if (_form.IsOrLowerState(State.Sky))
            {
                _form.State = State.Path;
                Sky = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSky>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Sky))
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
                _form.State = State.Completed;
                Path = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDPathList>.HandleReceivedTokenSkip()
        {
            if(_form.State == State.Path)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
