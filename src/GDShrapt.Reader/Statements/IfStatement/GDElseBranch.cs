namespace GDShrapt.Reader
{
    public sealed class GDElseBranch : GDIntendedNode,
        IKeywordReceiver<GDElseKeyword>,
        ITokenReceiver<GDColon>,
        IExpressionsReceiver,
        ITokenReceiver<GDNewLine>
    {
        internal GDElseKeyword ElseKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        internal GDColon Colon
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExpression Expression
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        internal GDNewLine NewLine
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }
        public GDStatementsList Statements
        {
            get => _form.Token4 ?? (_form.Token4 = new GDStatementsList(Intendation + 1));
        }

        enum State
        {
            Else, 
            Colon,
            Expression,
            NewLine,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDElseKeyword, GDColon, GDExpression, GDNewLine, GDStatementsList> _form;
        internal override GDTokensForm Form => _form;

        internal GDElseBranch(int intendation) 
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDElseKeyword, GDColon, GDExpression, GDNewLine, GDStatementsList>(this);
        }

        public GDElseBranch()
        {
            _form = new GDTokensForm<State, GDElseKeyword, GDColon, GDExpression, GDNewLine, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Else:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveKeyword(c, state);
                    break;
                case State.Colon:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveColon(c, state);
                    break;
                case State.Expression:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveExpression(c, state);
                    break;
                case State.NewLine:
                    if (!this.ResolveStyleToken(c, state))
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
            switch (_form.State)
            {
                case State.Else:
                case State.Colon:
                case State.Expression:
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
                    state.PopAndPassNewLine();
                    break;
            }
        }

        void IKeywordReceiver<GDElseKeyword>.HandleReceivedToken(GDElseKeyword token)
        {
            if (_form.State == State.Else)
            {
                _form.State = State.Colon;
                ElseKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDElseKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Else)
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
                _form.State = State.Expression;
                Colon = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Expression)
            {
                _form.State = State.Completed;
                Expression = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.Expression)
            {
                _form.State = State.NewLine;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDNewLine>.HandleReceivedToken(GDNewLine token)
        {
            if (_form.State == State.NewLine)
            {
                _form.State = State.Statements;
                NewLine = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDNewLine>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.NewLine)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
