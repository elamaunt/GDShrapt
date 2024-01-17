namespace GDShrapt.Reader
{
    public class GDSetAccessorBodyDeclaration : GDAccessorDeclaration,
        ITokenOrSkipReceiver<GDSetKeyword>,
        ITokenOrSkipReceiver<GDOpenBracket>,
        ITokenOrSkipReceiver<GDParameterDeclaration>,
        ITokenOrSkipReceiver<GDCloseBracket>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDStatementsList>
    {
        public GDSetKeyword SetKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDOpenBracket OpenBracket
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDParameterDeclaration Parameter
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public GDCloseBracket CloseBracket
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public GDColon Colon
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        public GDExpression Expression
        {
            get => _form.Token5;
            set => _form.Token5 = value;
        }

        public GDStatementsList Statements
        {
            get => _form.Token6 ?? (_form.Token6 = new GDStatementsList(Intendation + 1));
            set => _form.Token6 = value;
        }

        public enum State
        {
            Set,
            OpenBracket,
            Parameter,
            CloseBracket,
            Colon,
            Expression,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDSetKeyword, GDOpenBracket, GDParameterDeclaration, GDCloseBracket, GDColon, GDExpression, GDStatementsList> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDSetKeyword, GDOpenBracket, GDParameterDeclaration, GDCloseBracket, GDColon, GDExpression, GDStatementsList> TypedForm => _form;

        public GDSetAccessorBodyDeclaration()
        {
            _form = new GDTokensForm<State, GDSetKeyword, GDOpenBracket, GDParameterDeclaration, GDCloseBracket, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDSetAccessorBodyDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDSetKeyword, GDOpenBracket, GDParameterDeclaration, GDCloseBracket, GDColon, GDExpression, GDStatementsList>(this);
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
                case State.Set:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDSetKeyword>(c, state);
                    break;
                case State.OpenBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveOpenBracket(c, state);
                    break;
                case State.Parameter:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveParameter(c, state);
                    break;
                case State.CloseBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveCloseBracket(c, state);
                    break;
                case State.Colon:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveColon(c, state);
                    break;
                case State.Expression:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveExpression(c, state, Intendation);
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
                case State.Parameter:
                case State.CloseBracket:
                    _form.AddBeforeActiveToken(new GDNewLine());
                    break;

                case State.Set:
                case State.OpenBracket:
                case State.Colon:
                case State.Expression:
                case State.Statements:
                    _form.State = State.Completed;
                    state.PushAndPassNewLine(Statements);
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        void ITokenReceiver<GDSetKeyword>.HandleReceivedToken(GDSetKeyword token)
        {
            if (_form.State == State.Set)
            {
                SetKeyword = token;
                _form.State = State.OpenBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSetKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Set)
            {
                _form.State = State.OpenBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedToken(GDOpenBracket token)
        {
            if (_form.State == State.OpenBracket)
            {
                OpenBracket = token;
                _form.State = State.Parameter;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.OpenBracket)
            {
                _form.State = State.Parameter;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDParameterDeclaration>.HandleReceivedToken(GDParameterDeclaration token)
        {
            if (_form.State == State.Parameter)
            {
                Parameter = token;
                _form.State = State.CloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDParameterDeclaration>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Parameter)
            {
                _form.State = State.CloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedToken(GDCloseBracket token)
        {
            if (_form.State == State.CloseBracket)
            {
                CloseBracket = token;
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.CloseBracket)
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
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Expression)
            {
                Expression = token;
                _form.State = State.Statements;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Expression)
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