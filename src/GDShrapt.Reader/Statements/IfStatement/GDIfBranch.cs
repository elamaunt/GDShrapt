namespace GDShrapt.Reader
{
    public sealed class GDIfBranch : GDIntendedNode,
        IKeywordReceiver<GDIfKeyword>,
        IExpressionsReceiver,
        ITokenReceiver<GDColon>
    {
        internal GDIfKeyword IfKeyword
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
            If, 
            Condition, 
            Colon,
            Expression,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDIfKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> _form;
        internal override GDTokensForm Form => _form;

        internal GDIfBranch(int intendation) 
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDIfKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDIfBranch()
        {
            _form = new GDTokensForm<State, GDIfKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.If:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveKeyword(c, state);
                    break;
                case State.Colon:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveColon(c, state);
                    break;
                case State.Condition:
                case State.Expression:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveExpression(c, state);
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
                case State.If:
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
            return new GDIfBranch();
        }

        void IKeywordReceiver<GDIfKeyword>.HandleReceivedToken(GDIfKeyword token)
        {
            if (_form.State == State.If)
            {
                _form.State = State.Condition;
                IfKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDIfKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.If)
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
