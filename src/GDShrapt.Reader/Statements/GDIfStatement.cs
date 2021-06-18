namespace GDShrapt.Reader
{
    public sealed class GDIfStatement : GDStatement,
        IKeywordReceiver<GDIfKeyword>, 
        IExpressionsReceiver,
        IKeywordReceiver<GDElseKeyword>, 
        IKeywordReceiver<GDElifKeyword>,
        ITokenReceiver<GDColon>
    {
        internal GDSyntaxToken If
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

        public GDStatementsList TrueStatements
        {
            get => _form.Token3 ?? (_form.Token3 = new GDStatementsList(LineIntendation + 1));
        }

        internal GDSyntaxToken ElseOrElif
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        public GDStatementsList FalseStatements
        {
            get => _form.Token5 ?? (_form.Token5 = new GDStatementsList(LineIntendation + 1));
        }

        enum State
        {
            IfKeyword,
            Condition,
            Colon,
            //NewLine,
            TrueStatements,
            ElseOrElifKeyword,
            FalseStatements,
            Completed
        }

        readonly GDTokensForm<State, GDSyntaxToken, GDExpression, GDColon, GDStatementsList, GDSyntaxToken, GDStatementsList> _form = new GDTokensForm<State, GDSyntaxToken, GDExpression, GDColon, GDStatementsList, GDSyntaxToken, GDStatementsList>();
        internal override GDTokensForm Form => _form;

        internal GDIfStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDIfStatement()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c) /*&& _form.State != State.TrueStatements*/)
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.IfKeyword:
                    state.Push(new GDKeywordResolver<GDIfKeyword>(this));
                    state.PassChar(c);
                    break;
                case State.Condition:
                    state.Push(new GDExpressionResolver(this));
                    state.PassChar(c);
                    break;
                case State.Colon:
                    state.Push(new GDSingleCharTokenResolver<GDColon>(this));
                    state.PassChar(c);
                    break;
                case State.TrueStatements:
                    _form.State = State.ElseOrElifKeyword;
                    state.Push(TrueStatements);
                    state.PassChar(c);
                    break;
                case State.ElseOrElifKeyword:
                    // TODO: rework
                    // 'if' statement doesn't handle 'else' and 'elif' branches by yourself. It is managed by statement resolver.
                    // Just return control flow to previous node.
                    state.Pop();
                    state.PassChar(c);
                    break;
                case State.FalseStatements:
                    _form.State = State.Completed;
                    state.Push(TrueStatements);
                    state.PassChar(c);
                    break;
                default:
                    state.Pop();
                    state.PassChar(c);
                    break;

            }
        }

        // TODO: rework
        /*internal void HandleFalseStatements(GDReadingState state)
        {
            if (_form.State == State.FalseStatements)
            {
                state.Push(FalseStatements);
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }*/

        internal override void HandleNewLineChar(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Condition:
                case State.Colon:
               // case State.NewLine:
                    _form.State = State.FalseStatements;
                    _form.AddBeforeActiveToken(new GDNewLine());
                    break;
                case State.TrueStatements:
                    _form.State = State.ElseOrElifKeyword;
                    state.Push(TrueStatements);
                    state.PassNewLine();
                    break;
                case State.ElseOrElifKeyword:
                    _form.State = State.FalseStatements;
                    _form.AddBeforeActiveToken(new GDNewLine());
                    break;
                case State.FalseStatements:
                    _form.State = State.Completed;
                    state.Push(TrueStatements);
                    state.PassNewLine();
                    break;
                default:
                    state.Pop();
                    state.PassNewLine();
                    break;
            }
        }

        void IKeywordReceiver<GDIfKeyword>.HandleReceivedToken(GDIfKeyword token)
        {
            if (_form.State == State.IfKeyword)
            {
                If = token;
                _form.State = State.Condition;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDIfKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.IfKeyword)
            {
                _form.State = State.Condition;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDElseKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.ElseOrElifKeyword)
            {
                _form.State = State.FalseStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDElifKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.ElseOrElifKeyword)
            {
                _form.State = State.FalseStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDElseKeyword>.HandleReceivedToken(GDElseKeyword token)
        {
            if (_form.State == State.ElseOrElifKeyword)
            {
                ElseOrElif = token;
                _form.State = State.FalseStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDElifKeyword>.HandleReceivedToken(GDElifKeyword token)
        {
            if (_form.State == State.ElseOrElifKeyword)
            {
                ElseOrElif = token;
                _form.State = State.FalseStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                Colon = token;
                _form.State = State.TrueStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.TrueStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.Condition)
            {
                Condition = token;
                _form.State = State.Colon;
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

            throw new GDInvalidReadingStateException();
        }
    }
}