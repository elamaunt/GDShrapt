namespace GDShrapt.Reader
{
    public sealed class GDVariableDeclaration : GDClassMember, 
        IKeywordReceiver<GDConstKeyword>,
        IExportReceiver,
        IKeywordReceiver<GDOnreadyKeyword>,
        IKeywordReceiver<GDVarKeyword>,
        IIdentifierReceiver,
        ITypeReceiver,
        ITokenReceiver<GDColon>,
        ITokenReceiver<GDAssign>,
        IExpressionsReceiver,
        IKeywordReceiver<GDSetGetKeyword>,
        ITokenReceiver<GDComma>
    {
        internal GDConstKeyword ConstKeyword 
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        internal GDOnreadyKeyword OnreadyKeyword
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDExportDeclaration Export
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        
        internal GDVarKeyword VarKeyword
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public GDIdentifier Identifier
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }
        internal GDColon Colon
        {
            get => _form.Token5;
            set => _form.Token5 = value;
        }
        public GDType Type
        {
            get => _form.Token6;
            set => _form.Token6 = value;
        }
        internal GDAssign Assign
        {
            get => _form.Token7;
            set => _form.Token7 = value;
        }
        public GDExpression Initializer
        {
            get => _form.Token8;
            set => _form.Token8 = value;
        }
        internal GDSetGetKeyword SetGetKeyword
        {
            get => _form.Token9;
            set => _form.Token9 = value;
        }
        public GDIdentifier SetMethodIdentifier
        {
            get => _form.Token10;
            set => _form.Token10 = value;
        }

        internal GDComma Comma
        {
            get => _form.Token11;
            set => _form.Token11 = value;
        }

        public GDIdentifier GetMethodIdentifier
        {
            get => _form.Token12;
            set => _form.Token12 = value;
        }

        public bool IsExported => Export != null;
        public bool IsConstant => ConstKeyword != null;
        public bool HasOnReadyInitialization => OnreadyKeyword != null;

        enum State
        { 
            Const,
            Export,
            Onready,
            Var,
            Identifier,
            Colon,
            Type,
            Assign,
            Initializer,
            SetGet,
            SetMethod,
            Comma,
            GetMethod,
            Completed
        }

        readonly GDTokensForm<State, GDConstKeyword, GDOnreadyKeyword, GDExportDeclaration, GDVarKeyword, GDIdentifier, GDColon, GDType, GDAssign, GDExpression, GDSetGetKeyword, GDIdentifier, GDComma, GDIdentifier> _form;
        internal override GDTokensForm Form => _form;

        internal GDVariableDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDConstKeyword, GDOnreadyKeyword, GDExportDeclaration, GDVarKeyword, GDIdentifier, GDColon, GDType, GDAssign, GDExpression, GDSetGetKeyword, GDIdentifier, GDComma, GDIdentifier>(this);
        }

        public GDVariableDeclaration()
        {
            _form = new GDTokensForm<State, GDConstKeyword, GDOnreadyKeyword, GDExportDeclaration, GDVarKeyword, GDIdentifier, GDColon, GDType, GDAssign, GDExpression, GDSetGetKeyword, GDIdentifier, GDComma, GDIdentifier>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.Const:
                    state.PushAndPass(new GDKeywordResolver<GDConstKeyword>(this), c);
                    break;
                case State.Export:
                    state.PushAndPass(new GDExportResolver(this), c);
                    break;
                case State.Onready:
                    state.PushAndPass(new GDKeywordResolver<GDOnreadyKeyword>(this), c);
                    break;
                case State.Var:
                    state.PushAndPass(new GDKeywordResolver<GDVarKeyword>(this), c);
                    break;
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.Colon:
                    state.PushAndPass(new GDSingleCharTokenResolver<GDColon>(this), c);
                    break;
                case State.Type:
                    this.ResolveType(c, state);
                    break;
                case State.Assign:
                    state.Push(new GDSingleCharTokenResolver<GDAssign>(this));
                    state.PassChar(c);
                    break;
                case State.Initializer:
                    state.Push(new GDExpressionResolver(this));
                    state.PassChar(c);
                    break;
                case State.SetGet:
                    state.Push(new GDKeywordResolver<GDSetGetKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.SetMethod:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.Comma:
                    this.ResolveComma(c, state);
                    break;
                case State.GetMethod:
                    this.ResolveIdentifier(c, state);
                    break;
                default:
                    this.ResolveInvalidToken(c, state, x => x.IsNewLine());
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        void IKeywordReceiver<GDConstKeyword>.HandleReceivedToken(GDConstKeyword token)
        {
            if (_form.State == State.Const)
            {
                _form.State = State.Onready;
                ConstKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDConstKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Const)
            {
                _form.State = State.Onready;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDOnreadyKeyword>.HandleReceivedToken(GDOnreadyKeyword token)
        {
            if (_form.StateIndex <= (int)State.Onready)
            {
                _form.State = State.Export;
                OnreadyKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDOnreadyKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.StateIndex <= (int)State.Onready)
            {
                _form.State = State.Export;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExportReceiver.HandleReceivedExport(GDExportDeclaration token)
        {
            if (_form.StateIndex <= (int)State.Export)
            {
                Export = token;
                _form.State = State.Var;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExportReceiver.HandleReceivedExportSkip()
        {
            if (_form.StateIndex <= (int)State.Export)
            {
                _form.State = State.Var;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDVarKeyword>.HandleReceivedToken(GDVarKeyword token)
        {
            if (_form.StateIndex <= (int)State.Var)
            {
                _form.State = State.Identifier;
                VarKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDVarKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.StateIndex <= (int)State.Var)
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Type;
                Colon = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Assign;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDAssign>.HandleReceivedToken(GDAssign token)
        {
            if (_form.State == State.Assign)
            {
                _form.State = State.Initializer;
                Assign = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDAssign>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Assign)
            {
                _form.State = State.SetGet;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Initializer)
            {
                _form.State = State.SetGet;
                Initializer = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.Initializer)
            {
                _form.State = State.SetGet;

                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDSetGetKeyword>.HandleReceivedToken(GDSetGetKeyword token)
        {
            if (_form.State == State.SetGet)
            {
                _form.State = State.SetMethod;
                SetGetKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDSetGetKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.SetGet)
            {
                _form.State = State.SetMethod;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            if (_form.State == State.Comma)
            {
                _form.State = State.GetMethod;
                Comma = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDComma>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Comma)
            {
                _form.State = State.GetMethod;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Assign;
                Identifier = token;
                return;
            }

            if (_form.State == State.SetMethod)
            {
                _form.State = State.Comma;
                SetMethodIdentifier = token;
                return;
            }

            if (_form.State == State.GetMethod)
            {
                _form.State = State.Completed;
                GetMethodIdentifier = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedIdentifierSkip()
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Colon;
                return;
            }

            if (_form.State == State.SetMethod)
            {
                _form.State = State.Comma;
                return;
            }

            if (_form.State == State.GetMethod)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITypeReceiver.HandleReceivedToken(GDType token)
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Assign;
                Type = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITypeReceiver.HandleReceivedTypeSkip()
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Assign;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}