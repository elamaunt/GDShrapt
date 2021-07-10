namespace GDShrapt.Reader
{
    public sealed class GDExportDeclaration : GDNode,
        ITokenOrSkipReceiver<GDExportKeyword>,
        ITokenOrSkipReceiver<GDOpenBracket>,
        ITokenOrSkipReceiver<GDCloseBracket>
    {
        public GDExportKeyword ExportKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDOpenBracket OpenBracket
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDExportParametersList Parameters 
        { 
            get => _form.Token2 ?? (_form.Token2 = new GDExportParametersList());
            set => _form.Token2 = value;
        }
        public GDCloseBracket CloseBracket
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
        public override GDTokensForm Form => _form;
        public GDExportDeclaration()
        {
            _form = new GDTokensForm<State, GDExportKeyword, GDOpenBracket, GDExportParametersList, GDCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Export:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDExportKeyword>(c, state);
                    break;
                case State.OpenBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveOpenBracket(c, state);
                    break;
                case State.Parameters:
                    _form.State = State.CloseBracket;
                    state.PushAndPass(Parameters, c);
                    break;
                case State.CloseBracket:
                    if (!this.ResolveSpaceToken(c, state))
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

        void ITokenReceiver<GDExportKeyword>.HandleReceivedToken(GDExportKeyword token)
        {
            if (_form.State == State.Export)
            {
                _form.State = State.OpenBracket;
                ExportKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExportKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Export)
            {
                _form.State = State.OpenBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDOpenBracket>.HandleReceivedToken(GDOpenBracket token)
        {
            if (_form.State == State.OpenBracket)
            {
                _form.State = State.Parameters;
                OpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.OpenBracket)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedToken(GDCloseBracket token)
        {
            if (_form.State == State.CloseBracket)
            {
                _form.State = State.Completed;
                CloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.CloseBracket)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
