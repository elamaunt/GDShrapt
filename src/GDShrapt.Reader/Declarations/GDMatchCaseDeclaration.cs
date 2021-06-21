namespace GDShrapt.Reader
{
    public sealed class GDMatchCaseDeclaration : GDNode,
        ITokenReceiver<GDColon>
    {
        private int _lineIntendation;
        public GDExpressionsList Conditions { get => _form.Token0 ?? (_form.Token0 = new GDExpressionsList()); }

        internal GDColon Colon
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDStatementsList Statements { get => _form.Token2 ?? (_form.Token2 = new GDStatementsList(_lineIntendation + 1)); }

        enum State
        {
            Conditions,
            Colon,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDExpressionsList, GDColon, GDStatementsList> _form;
        internal override GDTokensForm Form => _form;
        internal GDMatchCaseDeclaration(int lineIntendation)
        {
            _lineIntendation = lineIntendation;
            _form = new GDTokensForm<State, GDExpressionsList, GDColon, GDStatementsList>(this);
        }

        public GDMatchCaseDeclaration()
        {
            _form = new GDTokensForm<State, GDExpressionsList, GDColon, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveStyleToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Conditions:
                    _form.State = State.Colon;
                    state.PushAndPass(Conditions, c);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
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
                case State.Conditions:
                case State.Colon:
                case State.Statements:
                    _form.State = State.Completed;
                    state.PushAndPassNewLine(Statements);
                    break;
                default:
                    state.PopAndPassNewLine();
                    break;
            }
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Statements;
                Colon = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Statements;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
