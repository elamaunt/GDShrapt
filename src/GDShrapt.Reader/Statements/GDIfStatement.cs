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

        State _state;
        enum State
        {
            IfKeyword,
            Condition,
            Colon,
            TrueStatements,
            ElseOrElifKeyword,
            FalseStatements,
            Completed
        }

        readonly GDTokensForm<GDSyntaxToken, GDExpression, GDColon, GDStatementsList, GDSyntaxToken, GDStatementsList> _form = new GDTokensForm<GDSyntaxToken, GDExpression, GDColon, GDStatementsList, GDSyntaxToken, GDStatementsList>();

        internal GDIfStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDIfStatement()
        {
            _form.AddBeforeToken1(new GDSpace() { Sequence = " " });
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeToken(state.Push(new GDSpace()), (int)_state);
                state.PassChar(c);
                return;
            }

            switch (_state)
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
                    _state = State.ElseOrElifKeyword;
                    state.Push(TrueStatements);
                    state.PassChar(c);
                    break;
                case State.ElseOrElifKeyword:
                    // 'if' statement doesn't handle 'else' and 'elif' branches by yourself. It is managed by statement resolver.
                    // Just return control flow to previous node.
                    state.Pop();
                    state.PassChar(c);
                    break;
                case State.FalseStatements:
                    _state = State.Completed;
                    state.Push(TrueStatements);
                    state.PassChar(c);
                    break;
                default:
                    state.Pop();
                    state.PassChar(c);
                    break;

            }


            // Old code
            /* if (IsSpace(c))
                 return;

             if (Condition == null)
             {
                 state.Push(new GDExpressionResolver(this));
                 state.PassChar(c);
                 return;
             }

             if (!_expressionEnded)
             {
                 if (c != ':')
                     return;

                 _expressionEnded = true;
                 return;
             }

             if (!_trueStatementsChecked)
             {
                 _trueStatementsChecked = true;
                 var statement = new GDExpressionStatement(LineIntendation + 1);
                 TrueStatements.Add(statement);
                 state.Push(statement);
                 state.PassChar(c);
                 return;
             }

             // 'if' statement doesn't handle 'else' and 'elif' branches by yourself. It is managed by statement resolver.
             // Just return control flow to previous node.
             state.Pop();
             state.PassChar(c);*/
        }


        internal void HandleFalseStatements(GDReadingState state)
        {
            state.Push(FalseStatements);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            switch (_state)
            {
                case State.TrueStatements:
                case State.ElseOrElifKeyword:
                case State.FalseStatements:
                    _form.AddBeforeToken(state.Push(new GDNewLine()), (int)_state);
                    break;
                default:
                    state.Pop();
                    state.PassLineFinish();
                    break;
            }


            // Old code
            /*if (!_trueStatementsChecked)
            {
                _trueStatementsChecked = true;
                state.Push(new GDStatementResolver(this, LineIntendation + 1));
                return;
            }

            state.Pop();
            state.PassLineFinish();*/
        }

        /*public override string ToString()
        {
            if (FalseStatements.Count == 0)
            {
                return $@"if {Condition}:
    {string.Join("\n\t", TrueStatements.Select(x => x.ToString()))}";
            }
            else
            {
                if (FalseStatements.Count == 1 && FalseStatements[0] is GDIfStatement statement)
                {
                    return $@"if {Condition}:
    {string.Join("\n\t", TrueStatements.Select(x => x.ToString()))}
el{statement}";
                }
                else
                {
                    return $@"if {Condition}:
    {string.Join("\n\t", TrueStatements.Select(x => x.ToString()))}
else:
    {string.Join("\n\t", FalseStatements.Select(x => x.ToString()))}";

                }
            }
        }*/

        public void HandleReceivedToken(GDIfKeyword token)
        {
            if (_state == State.IfKeyword)
            {
                If = token;
                _state = State.Condition;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        public void HandleReceivedKeywordSkip()
        {
            if (_state == State.IfKeyword)
            {
                _state = State.Condition;
                return;
            }

            if (_state == State.ElseOrElifKeyword)
            {
                _state = State.FalseStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        public void HandleReceivedToken(GDElseKeyword token)
        {
            if (_state == State.ElseOrElifKeyword)
            {
                ElseOrElif = token;
                _state = State.FalseStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        public void HandleReceivedToken(GDElifKeyword token)
        {
            if (_state == State.ElseOrElifKeyword)
            {
                ElseOrElif = token;
                _state = State.FalseStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        public void HandleReceivedToken(GDColon token)
        {
            if (_state == State.Colon)
            {
                Colon = token;
                _state = State.TrueStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip<B>()
        {
            if (_state == State.Colon)
            {
                _state = State.TrueStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_state == State.Condition)
            {
                Condition = token;
                _state = State.Colon;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_state == State.Condition)
            {
                _state = State.Colon;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDComment token)
        {
            _form.AddBeforeToken(token, (int)_state);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            _form.AddBeforeToken(token, (int)_state);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDSpace token)
        {
            _form.AddBeforeToken(token, (int)_state);
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            _form.AddBeforeToken(token, (int)_state);
        }
    }
}