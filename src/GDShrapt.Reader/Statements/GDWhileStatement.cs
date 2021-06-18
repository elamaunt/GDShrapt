
namespace GDShrapt.Reader
{
    public sealed class GDWhileStatement : GDStatement, 
        IKeywordReceiver<GDWhileKeyword>,
        IExpressionsReceiver,
        ITokenReceiver<GDColon>
    {
        internal GDWhileKeyword WhileKeyword
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
        internal GDNewLine NewLine
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public GDStatementsList Statements { get => _form.Token4 ?? (_form.Token4 = new GDStatementsList(LineIntendation + 1)); }

        enum State
        {
            While,
            Condition,
            Colon,
            NewLine,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDWhileKeyword, GDExpression, GDColon, GDNewLine, GDStatementsList> _form = new GDTokensForm<State, GDWhileKeyword, GDExpression, GDColon, GDNewLine, GDStatementsList>();
        internal override GDTokensForm Form => _form;

        internal GDWhileStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDWhileStatement()
        {
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
                case State.NewLine:
                    this.ResolveInvalidToken(c, state, x => x.IsNewLine());
                    break;
                case State.Statements:
                    _form.State = State.Completed;
                    state.Push(Statements);
                    state.PassChar(c);
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
                case State.NewLine:
                    _form.State = State.Statements;
                    NewLine =new GDNewLine();
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

        void IKeywordReceiver<GDWhileKeyword>.HandleReceivedToken(GDWhileKeyword token)
        {
            if (_form.State == State.While)
            {
                _form.State = State.Condition;
                WhileKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDWhileKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.While)
            {
                _form.State = State.Condition;
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

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.NewLine;
                Colon = token;
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
    }
}
