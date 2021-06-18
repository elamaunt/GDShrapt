namespace GDShrapt.Reader
{
    public sealed class GDMethodDeclaration : GDClassMember, 
        IKeywordReceiver<GDStaticKeyword>,
        IKeywordReceiver<GDFuncKeyword>,
        IIdentifierReceiver,
        ITokenReceiver<GDOpenBracket>,
        ITokenReceiver<GDCloseBracket>,
        IKeywordReceiver<GDReturnTypeKeyword>,
        ITypeReceiver,
        ITokenReceiver<GDColon>
    {
        internal GDStaticKeyword StaticKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        internal GDFuncKeyword FuncKeyword
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDIdentifier Identifier
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        internal GDOpenBracket OpenBracket
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }
        public GDParametersList Parameters { get => _form.Token4 ?? (_form.Token4 = new GDParametersList()); }
        internal GDCloseBracket CloseBracket
        {
            get => _form.Token5;
            set => _form.Token5 = value;
        }
        internal GDReturnTypeKeyword ReturnTypeKeyword
        {
            get => _form.Token6;
            set => _form.Token6 = value;
        }
        public GDType ReturnType
        {
            get => _form.Token7;
            set => _form.Token7 = value;
        }
        internal GDColon Colon
        {
            get => _form.Token8;
            set => _form.Token8 = value;
        }
        internal GDNewLine NewLine
        {
            get => _form.Token9;
            set => _form.Token9 = value;
        }

        public GDStatementsList Statements { get => _form.Token10 ?? (_form.Token10 = new GDStatementsList(Intendation + 1)); }

        public bool IsStatic => StaticKeyword != null;

        enum State
        {
            Static,
            Func,
            Identifier,
            OpenBracket,
            Parameters,
            CloseBracket,
            ReturnTypeKeyword,
            Type,
            Colon,
            NewLine,
            Statements,
            Completed,
        }

        readonly GDTokensForm<State, GDStaticKeyword, GDFuncKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket, GDReturnTypeKeyword, GDType, GDColon, GDNewLine, GDStatementsList> _form = new GDTokensForm<State, GDStaticKeyword, GDFuncKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket, GDReturnTypeKeyword, GDType, GDColon, GDNewLine, GDStatementsList>();
        internal override GDTokensForm Form => _form;

        internal GDMethodDeclaration(int intendation)
            : base(intendation)
        {

        }

        public GDMethodDeclaration()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c) && _form.State != State.Statements)
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.Static:
                    this.ResolveKeyword<GDStaticKeyword>(c, state);
                    break;
                case State.Func:
                    this.ResolveKeyword<GDFuncKeyword>(c, state);
                    break;
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.OpenBracket:
                    this.ResolveOpenBracket(c, state);
                    break;
                case State.Parameters:
                    _form.State = State.CloseBracket;
                    state.PushAndPass(Parameters, c);
                    break;
                case State.CloseBracket:
                    this.ResolveCloseBracket(c, state);
                    break;
                case State.ReturnTypeKeyword:
                    this.ResolveKeyword<GDReturnTypeKeyword>(c, state);
                    break;
                case State.Type:
                    this.ResolveType(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;
                case State.NewLine:
                    this.ResolveInvalidToken(c, state, x => x.IsNewLine());
                    break;
                case State.Statements:
                    _form.State = State.Completed;
                    state.PushAndPass(Statements, c);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_form.StateIndex <= (int)State.NewLine)
            {
                _form.State = State.Statements;
                NewLine = new GDNewLine();
                return;
            }

            if (_form.State == State.Statements)
            {
                _form.State = State.Completed;
                state.Push(Statements);
                state.PassNewLine();
                return;
            }

            state.PopAndPassNewLine();
        }

        void IKeywordReceiver<GDStaticKeyword>.HandleReceivedToken(GDStaticKeyword token)
        {
            if (_form.State == State.Static)
            {
                _form.State = State.Func;
                StaticKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDStaticKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Static)
            {
                _form.State = State.Func;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDFuncKeyword>.HandleReceivedToken(GDFuncKeyword token)
        {
            if (_form.State == State.Func)
            {
                _form.State = State.Identifier;
                FuncKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException(); 
        }

        void IKeywordReceiver<GDFuncKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Func)
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.OpenBracket;
                Identifier = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
        void IIdentifierReceiver.HandleReceivedIdentifierSkip()
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.OpenBracket;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedToken(GDOpenBracket token)
        {
            if (_form.State == State.OpenBracket)
            {
                _form.State = State.Parameters;
                OpenBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.OpenBracket)
            {
                _form.State = State.Parameters;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedToken(GDCloseBracket token)
        {
            if (_form.State == State.CloseBracket)
            {
                _form.State = State.ReturnTypeKeyword;
                CloseBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.CloseBracket)
            {
                _form.State = State.ReturnTypeKeyword;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDReturnTypeKeyword>.HandleReceivedToken(GDReturnTypeKeyword token)
        {
            if (_form.State == State.ReturnTypeKeyword)
            {
                _form.State = State.Type;
                ReturnTypeKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDReturnTypeKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.ReturnTypeKeyword)
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITypeReceiver.HandleReceivedToken(GDType token)
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Colon;
                ReturnType = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITypeReceiver.HandleReceivedTypeSkip()
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.NewLine;
                Colon = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.NewLine;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}