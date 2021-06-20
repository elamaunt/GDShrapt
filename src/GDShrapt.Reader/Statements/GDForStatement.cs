namespace GDShrapt.Reader
{
    public sealed class GDForStatement : GDStatement,
        IKeywordReceiver<GDForKeyword>,
        IKeywordReceiver<GDInKeyword>,
        ITokenReceiver<GDColon>,
        IExpressionsReceiver
    {
        internal GDForKeyword ForKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDIdentifier Variable
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        internal GDInKeyword InKeyword
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDExpression Collection
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }
        internal GDColon Colon
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        internal GDNewLine NewLine
        {
            get => _form.Token5;
            set => _form.Token5 = value;

        }


        public GDStatementsList Statements { get => _form.Token6?? (_form.Token6 = new GDStatementsList(LineIntendation + 1)); }

        enum State
        {
            For,
            Variable,
            In,
            Collection,
            Colon,
            NewLine,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDForKeyword, GDIdentifier, GDInKeyword, GDExpression, GDColon, GDNewLine, GDStatementsList> _form;
        internal override GDTokensForm Form => _form;

        internal GDForStatement(int lineIntendation)
            : base(lineIntendation)
        {
            _form = new GDTokensForm<State, GDForKeyword, GDIdentifier, GDInKeyword, GDExpression, GDColon, GDNewLine, GDStatementsList>(this);
        }

        public GDForStatement()
        {
            _form = new GDTokensForm<State, GDForKeyword, GDIdentifier, GDInKeyword, GDExpression, GDColon, GDNewLine, GDStatementsList>(this);
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
                case State.For:
                    state.Push(new GDKeywordResolver<GDForKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Variable:
                    if (IsIdentifierStartChar(c))
                    {
                        _form.State = State.In;
                        state.Push(Variable = new GDIdentifier());
                        state.PassChar(c);
                        return;
                    }

                    _form.AddBeforeActiveToken(state.Push(new GDInvalidToken(x => IsIdentifierStartChar(c))));
                    state.PassChar(c);
                    break;
                case State.In:
                    state.Push(new GDKeywordResolver<GDInKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Collection:
                    state.Push(new GDExpressionResolver(this));
                    state.PassChar(c);
                    break;
                case State.Colon:
                    state.Push(new GDSingleCharTokenResolver<GDColon>(this));
                    state.PassChar(c);
                    break;
                case State.NewLine:
                    this.ResolveInvalidToken(c, state, x => x.IsNewLine());
                    break;
                case State.Statements:
                    _form.State = State.Completed;
                    state.PushAndPass(Statements, c);
                    break;
                default:
                    state.Pop();
                    state.PassChar(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.In:
                case State.Variable:
                case State.Colon:
                case State.Collection:
                case State.NewLine:
                    _form.State = State.Statements;
                    NewLine = new GDNewLine();
                    break;
                case State.Statements:
                    _form.State = State.Completed;
                    state.Push(Statements);
                    state.PassNewLine();
                    break;
                default:
                    state.Pop();
                    state.PassNewLine();
                    break;
            }
        }


        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Collection)
            {
                _form.State = State.Colon;
                Collection = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.Collection)
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
                Colon = token;
                _form.State = State.NewLine;
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

        void IKeywordReceiver<GDInKeyword>.HandleReceivedToken(GDInKeyword token)
        {
            if (_form.State == State.In)
            {
                InKeyword = token;
                _form.State = State.Collection;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDInKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.In)
            {
                _form.State = State.Collection;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDForKeyword>.HandleReceivedToken(GDForKeyword token)
        {
            if (_form.State == State.For)
            {
                ForKeyword = token;
                _form.State = State.Variable;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDForKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.For)
            {
                _form.State = State.Variable;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
