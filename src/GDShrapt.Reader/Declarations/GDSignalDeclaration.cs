namespace GDShrapt.Reader
{
    public sealed class GDSignalDeclaration : GDIdentifiableClassMember,
        ITokenOrSkipReceiver<GDSignalKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDOpenBracket>,
        ITokenOrSkipReceiver<GDParametersList>,
        ITokenOrSkipReceiver<GDCloseBracket>
    {
        public GDSignalKeyword SignalKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public override GDIdentifier Identifier
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

        public enum State
        {
            Signal,
            Identifier,
            OpenBracket,
            Parameters,
            CloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDSignalKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public override bool IsStatic => false;
        public GDTokensForm<State, GDSignalKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket> TypedForm => _form;
        internal GDSignalDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDSignalKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket>(this);
        }

        public GDSignalDeclaration()
        {
            _form = new GDTokensForm<State, GDSignalKeyword, GDIdentifier, GDOpenBracket, GDParametersList, GDCloseBracket>(this);
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
                case State.Signal:
                    this.ResolveKeyword<GDSignalKeyword>(c, state);
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
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDSignalDeclaration();
        }

        void ITokenReceiver<GDSignalKeyword>.HandleReceivedToken(GDSignalKeyword token)
        {
            if (_form.IsOrLowerState(State.Signal))
            {
                _form.State = State.Identifier;
                SignalKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSignalKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Signal))
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
    }
}
