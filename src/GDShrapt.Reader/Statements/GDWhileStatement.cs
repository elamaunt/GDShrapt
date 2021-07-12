
namespace GDShrapt.Reader
{
    public sealed class GDWhileStatement : GDStatement, 
        ITokenOrSkipReceiver<GDWhileKeyword>,
        ITokenOrSkipReceiver<GDExpression>,
        ITokenOrSkipReceiver<GDColon>
    {
        public GDWhileKeyword WhileKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDExpression Condition
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDColon Colon
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
            get => _form.Token4 ?? (_form.Token4 = new GDStatementsList(LineIntendation + 1));
            set => _form.Token4 = value;
        }

        public enum State
        {
            While,
            Condition,
            Colon,
            Expression,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDWhileKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDWhileKeyword, GDExpression, GDColon, GDExpression, GDStatementsList> TypedForm => _form;

        internal GDWhileStatement(int lineIntendation)
            : base(lineIntendation)
        {
            _form = new GDTokensForm<State, GDWhileKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
        }

        public GDWhileStatement()
        {
            _form = new GDTokensForm<State, GDWhileKeyword, GDExpression, GDColon, GDExpression, GDStatementsList>(this);
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
                case State.While:
                    state.Push(new GDKeywordResolver<GDWhileKeyword>(this));
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
                case State.Expression:
                    this.ResolveExpression(c, state);
                    break;
                case State.Statements:
                    this.ResolveInvalidToken(c, state, x => x.IsNewLine());
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
                case State.Condition:
                case State.Colon:
                case State.Expression:
                case State.Statements:
                    _form.State = State.Completed;
                    state.PushAndPassNewLine(Statements);
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDWhileStatement();
        }

        void ITokenReceiver<GDWhileKeyword>.HandleReceivedToken(GDWhileKeyword token)
        {
            if (_form.IsOrLowerState(State.While))
            {
                _form.State = State.Condition;
                WhileKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDWhileKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.While))
            {
                _form.State = State.Condition;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExpression>.HandleReceivedToken(GDExpression token)
        {
            if (_form.IsOrLowerState(State.Condition))
            {
                _form.State = State.Colon;
                Condition = token;
                return;
            }

            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Completed;
                Expression = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExpression>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Condition))
            {
                _form.State = State.Colon;
                return;
            }

            if (_form.IsOrLowerState(State.Expression))
            {
                _form.State = State.Statements;
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
    }
}
