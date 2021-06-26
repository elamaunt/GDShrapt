namespace GDShrapt.Reader
{
    public sealed class GDElifBranch : GDIntendedNode,
        IKeywordReceiver<GDElifKeyword>,
        IExpressionsReceiver,
        ITokenReceiver<GDColon>
    {
        internal GDElifKeyword ElifKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDExpression Condition
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        internal GDColon Colon
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDExpression Expression
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
            Elif,
            Condition,
            Colon,
            Expression,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDElifKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> _form;
        internal override GDTokensForm Form => _form;

        internal GDElifBranch(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDElifKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDElifBranch()
        {
            _form = new GDTokensForm<State, GDElifKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveStyleToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Elif:
                    this.ResolveKeyword(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;
                case State.Condition:
                case State.Expression:
                    this.ResolveExpression(c, state);
                    break;
                case State.Statements:
                    this.ResolveInvalidToken(c, state, x => x.IsSpace() || x.IsNewLine());
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
                case State.Elif:
                case State.Condition:
                case State.Colon:
                case State.Expression:
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

        public override GDNode CreateEmptyInstance()
        {
            return new GDElifBranch();
        }

        void IKeywordReceiver<GDElifKeyword>.HandleReceivedToken(GDElifKeyword token)
        {
            if (_form.State == State.Elif)
            {
                _form.State = State.Condition;
                ElifKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDElifKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Elif)
            {
                _form.State = State.Condition;
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
            if (_form.State == State.Condition)
            {
                _form.State = State.Colon;
                Condition = token;
                return;
            }

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
            if (_form.State == State.Condition)
            {
                _form.State = State.Colon;
                return;
            }

            if (_form.State == State.Expression)
            {
                _form.State = State.Statements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
