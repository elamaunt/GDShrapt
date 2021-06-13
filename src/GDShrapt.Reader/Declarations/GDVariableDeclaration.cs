namespace GDShrapt.Reader
{
    public sealed class GDVariableDeclaration : GDClassMember, 
        IKeywordReceiver<GDConstKeyword>,
        IKeywordReceiver<GDExportKeyword>,
        IKeywordReceiver<GDOnreadyKeyword>,
        ITokenReceiver<GDColon>,
        ITokenReceiver<GDAssign>,
        IExpressionsReceiver,
        IKeywordReceiver<GDSetGetKeyword>
    {
        internal GDConstKeyword ConstKeyword 
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        internal GDExportDeclaration Export
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        internal GDOnreadyKeyword OnreadyKeyword
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDIdentifier Identifier
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }
        internal GDColon Colon
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }
        public GDType Type
        {
            get => _form.Token5;
            set => _form.Token5 = value;
        }
        internal GDAssign Assign
        {
            get => _form.Token6;
            set => _form.Token6 = value;
        }
        public GDExpression Initializer
        {
            get => _form.Token7;
            set => _form.Token7 = value;
        }
        internal GDSetGetKeyword SetGetKeyword
        {
            get => _form.Token8;
            set => _form.Token8 = value;
        }
        public GDIdentifier GetMethodIdentifier
        {
            get => _form.Token9;
            set => _form.Token9 = value;
        }
        public GDIdentifier SetMethodIdentifier
        {
            get => _form.Token10;
            set => _form.Token10 = value;
        }

        public bool IsExported => ExportKeyword != null;
        public bool IsConstant => ConstKeyword != null;
        public bool HasOnReadyInitialization => OnreadyKeyword != null;

        enum State
        { 
            Const,
            Export,
            Onready,
            Identifier,
            Colon,
            Type,
            Assign,
            Initializer,
            SetGet,
            GetMethod,
            SetMethod,
            Completed
        }

        readonly GDTokensForm<State, GDConstKeyword, GDExportKeyword, GDOnreadyKeyword, GDIdentifier, GDColon, GDType, GDAssign, GDExpression, GDSetGetKeyword, GDIdentifier, GDIdentifier> _form = new GDTokensForm<State, GDConstKeyword, GDExportKeyword, GDOnreadyKeyword, GDIdentifier, GDColon, GDType, GDAssign, GDExpression, GDSetGetKeyword, GDIdentifier, GDIdentifier>();
        internal override GDTokensForm Form => _form;

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
                    state.Push(new GDKeywordResolver<GDConstKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Export:
                    state.Push(new GDKeywordResolver<GDExportKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Onready:
                    state.Push(new GDKeywordResolver<GDOnreadyKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Identifier:
                    if (IsIdentifierStartChar(c))
                    {
                        _form.State = State.Assign;
                        state.Push(Identifier = new GDIdentifier());
                        state.PassChar(c);
                    }
                    else
                    {
                        _form.State = State.Colon;
                        goto case State.Colon;
                    }
                    break;
                case State.Colon:
                    state.Push(new GDSingleCharTokenResolver<GDColon>(this));
                    state.PassChar(c);
                    break;
                case State.Type:
                    if (IsIdentifierStartChar(c))
                    {
                        _form.State = State.Assign;
                        state.Push(Type = new GDType());
                        state.PassChar(c);
                    }
                    else
                    {
                        _form.State = State.Assign;
                        goto case State.Assign;
                    }
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
                case State.GetMethod:
                    if (IsIdentifierStartChar(c))
                    {
                        _form.State = State.SetMethod;
                        state.Push(Identifier = new GDIdentifier());
                    }
                    else
                    {
                        _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(' ', '\n')));
                    }

                    state.PassChar(c);
                    break;
                case State.SetMethod:
                    if (IsIdentifierStartChar(c))
                    {
                        _form.State = State.Completed;
                        state.Push(Identifier = new GDIdentifier());
                    }
                    else
                    {
                        _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(' ', '\n')));
                    }

                    state.PassChar(c);
                    break;
                default:
                    state.Pop();
                    state.PassChar(c);
                    break;
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        void IKeywordReceiver<GDConstKeyword>.HandleReceivedToken(GDConstKeyword token)
        {
            if (_form.State == State.Const)
            {
                _form.State = State.Export;
                ConstKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDConstKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Const)
            {
                _form.State = State.Export;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDExportKeyword>.HandleReceivedToken(GDExportKeyword token)
        {
            if (_form.State == State.Export)
            {
                _form.State = State.Onready;
                ExportKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDExportKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Export)
            {
                _form.State = State.Onready;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDOnreadyKeyword>.HandleReceivedToken(GDOnreadyKeyword token)
        {
            if (_form.State == State.Onready)
            {
                _form.State = State.Identifier;
                OnreadyKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDOnreadyKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Onready)
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
                _form.State = State.Type;
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
                _form.State = State.Initializer;
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
    }
}