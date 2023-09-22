namespace GDShrapt.Reader
{
    public class GDMethodExpression : GDExpression,
         ITokenOrSkipReceiver<GDFuncKeyword>,
         ITokenOrSkipReceiver<GDIdentifier>,
         ITokenOrSkipReceiver<GDOpenBracket>,
         ITokenOrSkipReceiver<GDParametersList>,
         ITokenOrSkipReceiver<GDCloseBracket>,
         ITokenOrSkipReceiver<GDReturnTypeKeyword>,
         ITokenOrSkipReceiver<GDType>,
         ITokenOrSkipReceiver<GDColon>,
         ITokenOrSkipReceiver<GDExpression>,
         ITokenOrSkipReceiver<GDStatementsList>
    {
        internal int Intendation { get; }

        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Method);
        public GDFuncKeyword FuncKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDIdentifier Identifier
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDOpenBracket OpenBracket
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDParametersList Parameters
        {
            get => _form.Token3 ?? (_form.Token3 = new GDParametersList());
            set => _form.Token3 = value;
        }
        public GDCloseBracket CloseBracket
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }
        public GDReturnTypeKeyword ReturnTypeKeyword
        {
            get => _form.Token5;
            set => _form.Token5 = value;
        }
        public GDType ReturnType
        {
            get => _form.Token6;
            set => _form.Token6 = value;
        }
        public GDColon Colon
        {
            get => _form.Token7;
            set => _form.Token7 = value;
        }
        public GDExpression Expression
        {
            get => _form.Token8;
            set => _form.Token8 = value;
        }
        public GDStatementsList Statements
        {
            get => _form.Token9 ?? (_form.Token9 = new GDStatementsList(Intendation + 1));
            set => _form.Token9 = value;
        }

        public enum State
        {
            Func,
            Identifier,
            OpenBracket,
            Parameters,
            CloseBracket,
            ReturnTypeKeyword,
            Type,
            Colon,
            Expression,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDFuncKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket, GDReturnTypeKeyword, GDType, GDColon, GDExpression, GDStatementsList> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDFuncKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket, GDReturnTypeKeyword, GDType, GDColon, GDExpression, GDStatementsList> TypedForm => _form;

        internal GDMethodExpression(int intendation)
        {
            Intendation = intendation;
            _form = new GDTokensForm<State, GDFuncKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket, GDReturnTypeKeyword, GDType, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDMethodExpression()
        {
            _form = new GDTokensForm<State, GDFuncKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket, GDReturnTypeKeyword, GDType, GDColon, GDExpression, GDStatementsList>(this);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDMethodExpression();
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c) && _form.State != State.Parameters)
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
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

                case State.Expression:
                    this.ResolveExpression(c, state);
                    break;
                case State.Statements:
                    if (c.IsExpressionStopChar())
                    {
                        state.PopAndPass(c);
                        return;
                    }

                    this.HandleAsInvalidToken(c, state, x => x.IsSpace() || x.IsNewLine());
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                state.PushAndPassNewLine(Parameters);
                return;
            }

            if (_form.StateIndex <= (int)State.Statements)
            {
                _form.State = State.Completed;
                state.PushAndPassNewLine(Statements);
                return;
            }

            state.PopAndPassNewLine();
        }

        void ITokenReceiver<GDFuncKeyword>.HandleReceivedToken(GDFuncKeyword token)
        {
            if (_form.IsOrLowerState(State.Func))
            {
                _form.State = State.Identifier;
                FuncKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDFuncKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Func))
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDIdentifier>.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.OpenBracket;
                Identifier = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.OpenBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedToken(GDOpenBracket token)
        {
            if (_form.IsOrLowerState(State.OpenBracket))
            {
                _form.State = State.Parameters;
                OpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.OpenBracket))
            {
                _form.State = State.Parameters;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedToken(GDCloseBracket token)
        {
            if (_form.IsOrLowerState(State.CloseBracket))
            {
                _form.State = State.ReturnTypeKeyword;
                CloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.CloseBracket))
            {
                _form.State = State.ReturnTypeKeyword;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDReturnTypeKeyword>.HandleReceivedToken(GDReturnTypeKeyword token)
        {
            if (_form.IsOrLowerState(State.ReturnTypeKeyword))
            {
                _form.State = State.Type;
                ReturnTypeKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDReturnTypeKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.ReturnTypeKeyword))
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDType>.HandleReceivedToken(GDType token)
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Colon;
                ReturnType = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDType>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Colon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Expression;
                Colon = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Colon))
            {
                _form.State = State.Expression;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDParametersList>.HandleReceivedToken(GDParametersList token)
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                Parameters = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDParametersList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Statements;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Statements;
                Expression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDStatementsList>.HandleReceivedToken(GDStatementsList token)
        {
            if (_form.IsOrLowerState(State.Statements))
            {
                _form.State = State.Completed;
                Statements = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDStatementsList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Statements))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
