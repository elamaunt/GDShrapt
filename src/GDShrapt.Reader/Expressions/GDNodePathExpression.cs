namespace GDShrapt.Reader
{
    public sealed class GDNodePathExpression : GDExpression,
        ITokenReceiver<GDAt>,
        IStringReceiver
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.NodePath);

        internal GDAt At
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDString Path
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        enum State
        {
            At, 
            Path,
            Completed
        }

        readonly GDTokensForm<State, GDAt, GDString> _form;
        internal override GDTokensForm Form => _form;
        public GDNodePathExpression()
        {
            _form = new GDTokensForm<State, GDAt, GDString>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.At:
                    if (this.ResolveStyleToken(c, state))
                        return;
                    this.ResolveAt(c, state);
                    break;
                case State.Path:
                    this.ResolveString(c, state);
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

        void ITokenReceiver<GDAt>.HandleReceivedToken(GDAt token)
        {
            if (_form.State == State.At)
            {
                _form.State = State.Path;
                At = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDAt>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.At)
            {
                _form.State = State.Path;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IStringReceiver.HandleReceivedToken(GDString token)
        {
            if (_form.State == State.Path)
            {
                _form.State = State.Completed;
                Path = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IStringReceiver.HandleReceivedStringSkip()
        {
            if(_form.State == State.Path)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
