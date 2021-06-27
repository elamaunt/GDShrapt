namespace GDShrapt.Reader
{
    public sealed class GDClassNameAtribute : GDClassAtribute,
        IKeywordReceiver<GDClassNameKeyword>,
        IIdentifierReceiver,
        ITokenReceiver<GDComma>,
        IStringReceiver
    {
        public GDClassNameKeyword ClassNameKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDIdentifier Identifier 
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public GDComma Comma
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public GDString Icon
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        enum State
        {
            ClassName,
            Identifier,
            Comma,
            Icon,
            Completed
        }

        readonly GDTokensForm<State, GDClassNameKeyword, GDIdentifier, GDComma, GDString> _form;
        public override GDTokensForm Form => _form;

        public GDClassNameAtribute()
        {
            _form = new GDTokensForm<State, GDClassNameKeyword, GDIdentifier, GDComma, GDString>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveStyleToken(c, state))
                return;

            switch (_form.State)
            {
                case State.ClassName:
                    this.ResolveKeyword(c, state);
                    break;
                case State.Identifier:
                    this.ResolveIdentifier(c, state);
                    break;
                case State.Comma:
                    this.ResolveComma(c, state);
                    break;
                case State.Icon:
                    this.ResolveString(c, state);
                    break;
                default:
                    this.ResolveInvalidToken(c, state, x => x.IsNewLine());
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDClassNameAtribute();
        }

        void IKeywordReceiver<GDClassNameKeyword>.HandleReceivedToken(GDClassNameKeyword token)
        {
            if (_form.State == State.ClassName)
            {
                _form.State = State.Identifier;
                ClassNameKeyword = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IKeywordReceiver<GDClassNameKeyword>.HandleReceivedKeywordSkip()
        {
            if (_form.State == State.ClassName)
            {
                _form.State = State.Identifier;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedToken(GDIdentifier token)
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Comma;
                Identifier = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IIdentifierReceiver.HandleReceivedIdentifierSkip()
        {
            if (_form.State == State.Identifier)
            {
                _form.State = State.Comma;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            if (_form.State == State.Comma)
            {
                _form.State = State.Icon;
                Comma = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void ITokenReceiver<GDComma>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Comma)
            {
                _form.State = State.Icon;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IStringReceiver.HandleReceivedToken(GDString token)
        {
            if (_form.State == State.Icon)
            {
                _form.State = State.Completed;
                Icon = token;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IStringReceiver.HandleReceivedStringSkip()
        {
            if (_form.State == State.Icon)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}