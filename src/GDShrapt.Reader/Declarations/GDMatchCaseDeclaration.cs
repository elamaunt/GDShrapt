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

        internal GDNewLine NewLine
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public GDStatementsList Statements { get => _form.Token3 ?? (_form.Token3 = new GDStatementsList(_lineIntendation + 1)); }

        enum State
        {
            Conditions,
            Colon,
            NewLine,
            Statements,
            Completed
        }

        readonly GDTokensForm<State, GDExpressionsList, GDColon, GDNewLine, GDStatementsList> _form;
        internal override GDTokensForm Form => _form;
        internal GDMatchCaseDeclaration(int lineIntendation)
        {
            _lineIntendation = lineIntendation;
            _form = new GDTokensForm<State, GDExpressionsList, GDColon, GDNewLine, GDStatementsList>(this);
        }

        public GDMatchCaseDeclaration()
        {
            _form = new GDTokensForm<State, GDExpressionsList, GDColon, GDNewLine, GDStatementsList>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Conditions:
                    if (!this.ResolveStyleToken(c, state))
                    {
                        _form.State = State.Colon;
                        state.PushAndPass(Conditions, c);
                    }
                    break;
                case State.Colon:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveColon(c, state);
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
                case State.Conditions:
                case State.Colon:
                case State.NewLine:
                    _form.State = State.Statements;
                    NewLine = new GDNewLine();
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
