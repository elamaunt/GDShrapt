namespace GDShrapt.Reader
{
    public class GDGetAccessorBodyDeclaration : GDAccessorDeclaration,
        ITokenOrSkipReceiver<GDGetKeyword>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDStatementsList>
    {
        public GDGetKeyword GetKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDColon Colon
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDStatementsList Statements
        {
            get => _form.Token2 ?? (_form.Token2 = new GDStatementsList(Intendation + 1));
            set => _form.Token2 = value;
        }

        public enum State
        {
            Get,
            Colon,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDGetKeyword, GDColon, GDStatementsList> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDGetKeyword, GDColon, GDStatementsList> TypedForm => _form;

        public GDGetAccessorBodyDeclaration()
        {
            _form = new GDTokensForm<State, GDGetKeyword, GDColon, GDStatementsList>(this);
        }

        public GDGetAccessorBodyDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDGetKeyword, GDColon, GDStatementsList>(this);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDGetAccessorMethodDeclaration();
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
                case State.Get:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDGetKeyword>(c, state);
                    break;
                case State.Colon:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveColon(c, state);
                    break;
                case State.Statements:
                    this.HandleAsInvalidToken(c, state, x => x.IsSpace() || x.IsNewLine());
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Get:
                case State.Colon:
                case State.Statements:
                    _form.State = State.Completed;
                    state.PushAndPassNewLine(Statements);
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        void ITokenReceiver<GDGetKeyword>.HandleReceivedToken(GDGetKeyword token)
        {
            if (_form.State == State.Get)
            {
                GetKeyword = token;
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDGetKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Get)
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                Colon = token;
                _form.State = State.Statements;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Statements;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDStatementsList>.HandleReceivedToken(GDStatementsList token)
        {
            if (_form.State == State.Statements)
            {
                Statements = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDStatementsList>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Statements)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}