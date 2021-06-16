namespace GDShrapt.Reader
{
    public sealed class GDParameterDeclaration : GDNode,
        IIdentifierReceiver,
        ITokenReceiver<GDColon>,
        ITypeReceiver
    {
        public GDIdentifier Identifier
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        internal GDColon Colon
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDType Type
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        enum State
        {
            Identifier,
            Colon,
            Type,
            Completed
        }

        readonly GDTokensForm<State, GDIdentifier, GDColon, GDType> _form = new GDTokensForm<State, GDIdentifier, GDColon, GDType>();
        internal override GDTokensForm Form => throw new System.NotImplementedException();
        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveStyleToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.Colon:
                    this.ResolveColon(c, state);
                    break;
                case State.Type:
                    this.ResolveType(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        void IIdentifierReceiver.HandleReceivedToken(GDIdentifier token)
        {
            if(_form.State == State.Identifier)
            {
                _form.State = State.Colon;
                Identifier = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedIdentifierSkip()
        {
            if (_form.State == State.Identifier)
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
                _form.State = State.Type;
                Colon = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Colon)
            {
                _form.State = State.Type;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITypeReceiver.HandleReceivedToken(GDType token)
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Completed;
                Type = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITypeReceiver.HandleReceivedTypeSkip()
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}