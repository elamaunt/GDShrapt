namespace GDShrapt.Reader
{
    public sealed class GDExportDeclaration : GDNode,
        IKeywordReceiver<GDExportKeyword>,
        ITokenReceiver<GDOpenBracket>,
        ITokenReceiver<GDCloseBracket>
    {
        internal GDExportKeyword ExportKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        internal GDOpenBracket OpenBracket
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDExportParametersList Parameters { get => _form.Token2 ?? (_form.Token2 = new GDExportParametersList()); }

        internal GDCloseBracket CloseBracket
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        enum State
        {
            Export,
            OpenBracket,
            Parameters,
            CloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDExportKeyword, GDOpenBracket, GDExportParametersList, GDCloseBracket> _form;
        internal override GDTokensForm Form => _form;
        public GDExportDeclaration()
        {
            _form = new GDTokensForm<State, GDExportKeyword, GDOpenBracket, GDExportParametersList, GDCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Export:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveKeyword(c, state);
                    break;
                case State.OpenBracket:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveOpenBracket(c, state);
                    break;
                case State.Parameters:
                    _form.State = State.CloseBracket;
                    state.PushAndPass(Parameters, c);
                    break;
                case State.CloseBracket:
                    if (!this.ResolveStyleToken(c, state))
                        this.ResolveCloseBracket(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_form.State == State.Parameters)
            {
                _form.State = State.CloseBracket;
                state.PushAndPassNewLine(Parameters);
                return;
            }

            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDExportDeclaration();
        }

        void IKeywordReceiver<GDExportKeyword>.HandleReceivedToken(GDExportKeyword token)
        {
            if (_form.State == State.Export)
            {
                _form.State = State.OpenBracket;
                ExportKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDExportKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.Export)
            {
                _form.State = State.OpenBracket;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedToken(GDOpenBracket token)
        {
            if (_form.State == State.OpenBracket)
            {
                _form.State = State.Parameters;
                OpenBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.OpenBracket)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedToken(GDCloseBracket token)
        {
            if (_form.State == State.CloseBracket)
            {
                _form.State = State.Completed;
                CloseBracket = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.CloseBracket)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
