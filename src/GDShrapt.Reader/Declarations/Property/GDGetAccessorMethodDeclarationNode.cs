namespace GDShrapt.Reader
{
    public class GDGetAccessorMethodDeclarationNode : GDAccessorDeclarationNode,
        ITokenOrSkipReceiver<GDGetKeyword>,
        ITokenOrSkipReceiver<GDAssign>,
        ITokenOrSkipReceiver<GDIdentifier>
    {
        public GDGetKeyword GetKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDAssign Assign
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDIdentifier Identifier
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public enum State
        { 
            Get,
            Assign,
            Identifier,
            Completed
        }

        readonly GDTokensForm<State, GDGetKeyword, GDAssign, GDIdentifier> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDGetKeyword, GDAssign, GDIdentifier> TypedForm => _form;

        public GDGetAccessorMethodDeclarationNode()
        {
            _form = new GDTokensForm<State, GDGetKeyword, GDAssign, GDIdentifier>(this);
        }

        public GDGetAccessorMethodDeclarationNode(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDGetKeyword, GDAssign, GDIdentifier>(this);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDGetAccessorMethodDeclarationNode();
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Get:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDGetKeyword>(c, state);
                    break;
                case State.Assign:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveAssign(c, state);
                    break;
                case State.Identifier:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveIdentifier(c, state);
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

        void ITokenReceiver<GDGetKeyword>.HandleReceivedToken(GDGetKeyword token)
        {
            if (_form.State == State.Get)
            {
                GetKeyword = token;
                _form.State = State.Assign;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDGetKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Get)
            {
                _form.State = State.Assign;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDAssign>.HandleReceivedToken(GDAssign token)
        {
            if (_form.State == State.Assign)
            {
                Assign = token;
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAssign>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Assign)
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDIdentifier>.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.State == State.Identifier)
            {
                Identifier = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}