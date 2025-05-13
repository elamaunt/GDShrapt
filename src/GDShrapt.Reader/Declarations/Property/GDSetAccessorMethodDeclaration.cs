namespace GDShrapt.Reader
{
    public class GDSetAccessorMethodDeclaration : GDAccessorDeclaration,
        ITokenOrSkipReceiver<GDSetKeyword>,
        ITokenOrSkipReceiver<GDAssign>,
        ITokenOrSkipReceiver<GDIdentifier>
    {
        public GDSetKeyword SetKeyword
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
            Set,
            Assign,
            Identifier,
            Completed
        }

        readonly GDTokensForm<State, GDSetKeyword, GDAssign, GDIdentifier> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDSetKeyword, GDAssign, GDIdentifier> TypedForm => _form;

        public GDSetAccessorMethodDeclaration()
        {
            _form = new GDTokensForm<State, GDSetKeyword, GDAssign, GDIdentifier>(this);
        }

        public GDSetAccessorMethodDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDSetKeyword, GDAssign, GDIdentifier>(this);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDSetAccessorMethodDeclaration();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Set:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDSetKeyword>(c, state);
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

        void ITokenReceiver<GDSetKeyword>.HandleReceivedToken(GDSetKeyword token)
        {
            if (_form.State == State.Set)
            {
                SetKeyword = token;
                _form.State = State.Assign;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSetKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Set)
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