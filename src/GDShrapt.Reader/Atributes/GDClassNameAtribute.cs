namespace GDShrapt.Reader
{
    public sealed class GDClassNameAtribute : GDClassAtribute,
        ITokenOrSkipReceiver<GDClassNameKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDComma>,
        ITokenOrSkipReceiver<GDString>
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

        public enum State
        {
            ClassName,
            Identifier,
            Comma,
            Icon,
            Completed
        }

        readonly GDTokensForm<State, GDClassNameKeyword, GDIdentifier, GDComma, GDString> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDClassNameKeyword, GDIdentifier, GDComma, GDString> TypedForm => _form;

        public GDClassNameAtribute()
        {
            _form = new GDTokensForm<State, GDClassNameKeyword, GDIdentifier, GDComma, GDString>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            switch (_form.State)
            {
                case State.ClassName:
                    this.ResolveKeyword<GDClassNameKeyword>(c, state);
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
            return new GDClassNameAtribute();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDClassNameKeyword>.HandleReceivedToken(GDClassNameKeyword token)
        {
            if (_form.IsOrLowerState(State.ClassName))
            {
                _form.State = State.Identifier;
                ClassNameKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDClassNameKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.ClassName))
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
                _form.State = State.Comma;
                Identifier = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.Comma;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            if (_form.IsOrLowerState(State.Comma))
            {
                _form.State = State.Icon;
                Comma = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDComma>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Comma))
            {
                _form.State = State.Icon;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDString>.HandleReceivedToken(GDString token)
        {
            if (_form.IsOrLowerState(State.Icon))
            {
                _form.State = State.Completed;
                Icon = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDString>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Icon))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}