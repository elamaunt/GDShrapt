namespace GDShrapt.Reader
{
    public sealed class GDNodePathExpression : GDExpression,
        ITokenOrSkipReceiver<GDAt>,
        ITokenOrSkipReceiver<GDPathList>
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.NodePath);

        public GDAt At
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
            At, 
            Path,
            Completed
        }

        readonly GDTokensForm<State, GDAt, GDPathList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDAt, GDPathList> TypedForm => _form;
        public GDNodePathExpression()
        {
            _form = new GDTokensForm<State, GDAt, GDPathList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.At:
                    if (this.ResolveSpaceToken(c, state))
                        return;
                    this.ResolveAt(c, state);
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

        void ITokenReceiver<GDAt>.HandleReceivedToken(GDAt token)
        {
            if (_form.IsOrLowerState(State.At))
            {
                _form.State = State.Path;
                At = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAt>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.At))
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
