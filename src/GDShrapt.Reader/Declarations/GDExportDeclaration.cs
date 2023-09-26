namespace GDShrapt.Reader
{
    // Legacy
   /* public sealed class GDExportDeclaration : GDNode,
        ITokenOrSkipReceiver<GDAt>,
        ITokenOrSkipReceiver<GDExportKeyword>,
        ITokenOrSkipReceiver<GDOpenBracket>,
        ITokenOrSkipReceiver<GDDataParametersList>,
        ITokenOrSkipReceiver<GDCloseBracket>
    {
        public GDAt At
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDExportKeyword ExportKeyword
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDOpenBracket OpenBracket
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }
        public GDDataParametersList Parameters 
        { 
            get => _form.Token3 ?? (_form.Token3 = new GDDataParametersList());
            set => _form.Token3 = value;
        }
        public GDCloseBracket CloseBracket
        {
            get => _form.Token4;
            set => _form.Token4 = value;
        }

        public enum State
        {
            At,
            Export,
            OpenBracket,
            Parameters,
            CloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDAt, GDExportKeyword, GDOpenBracket, GDDataParametersList, GDCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDAt, GDExportKeyword, GDOpenBracket, GDDataParametersList, GDCloseBracket> TypedForm => _form;
       
        public GDExportDeclaration()
        {
            _form = new GDTokensForm<State, GDAt, GDExportKeyword, GDOpenBracket, GDDataParametersList, GDCloseBracket>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.At:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveAt(c, state);
                    break;
                case State.Export:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDExportKeyword>(c, state);
                    else
                        state.Pop();
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
            if (_form.IsOrLowerState(State.Parameters))
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

        void ITokenReceiver<GDAt>.HandleReceivedToken(GDAt token)
        {
            if (_form.IsOrLowerState(State.At))
            {
                _form.State = State.Export;
                At = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAt>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.At))
            {
                _form.State = State.Export;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDExportKeyword>.HandleReceivedToken(GDExportKeyword token)
        {
            if (_form.IsOrLowerState(State.Export))
            {
                _form.State = State.OpenBracket;
                ExportKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExportKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Export))
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
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDCloseBracket>.HandleReceivedToken(GDCloseBracket token)
        {
            if (_form.IsOrLowerState(State.CloseBracket))
            {
                _form.State = State.Completed;
                CloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.CloseBracket))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDDataParametersList>.HandleReceivedToken(GDDataParametersList token)
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                Parameters = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDDataParametersList>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Parameters))
            {
                _form.State = State.CloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }
    }*/
}
