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
            //_form.AddBeforeToken1(new GDSpace() { Sequence = " " });
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
            if (_form.State == State.FalseStatements)
            {
                state.Push(FalseStatements);
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Condition:
                case State.Colon:
                    _form.State = State.FalseStatements;
                    _form.AddBeforeActiveToken(new GDNewLine());
                    break;
                case State.TrueStatements:
                    _form.State = State.ElseOrElifKeyword;
                    state.Push(TrueStatements);
                    state.PassLineFinish();
                    break;
                case State.ElseOrElifKeyword:
                    _form.State = State.FalseStatements;
                    _form.AddBeforeActiveToken(new GDNewLine());
                    break;
                case State.FalseStatements:
                    _form.State = State.Completed;
                    state.Push(TrueStatements);
                    state.PassLineFinish();
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
            if (_form.State == State.IfKeyword)
            {
                If = token;
                _form.State = State.Condition;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        public void HandleReceivedKeywordSkip()
        {
            if (_form.State == State.IfKeyword)
            {
                _form.State = State.Condition;
                return;
            }

            if (_form.State == State.ElseOrElifKeyword)
            {
                _form.State = State.FalseStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        public void HandleReceivedToken(GDElseKeyword token)
        {
            if (_form.State == State.ElseOrElifKeyword)
            {
                ElseOrElif = token;
                _form.State = State.FalseStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        public void HandleReceivedToken(GDElifKeyword token)
        {
            if (_form.State == State.ElseOrElifKeyword)
            {
                ElseOrElif = token;
                _form.State = State.FalseStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        public void HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                Colon = token;
                _form.State = State.TrueStatements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip<B>()
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

        void IStyleTokensReceiver.HandleReceivedToken(GDComment token)
        {
            _form.AddBeforeActiveToken(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            _form.AddBeforeActiveToken(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDSpace token)
        {
            _form.AddBeforeActiveToken(token);
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            _form.AddBeforeActiveToken(token);
        }
    }
}