namespace GDShrapt.Reader
{
    public sealed class GDClassNameAttribute : GDClassAttribute,
        ITokenOrSkipReceiver<GDClassNameKeyword>,
        ITokenOrSkipReceiver<GDIdentifier>
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

        public enum State
        {
            ClassName,
            Identifier,
            Completed
        }

        readonly GDTokensForm<State, GDClassNameKeyword, GDIdentifier, GDComma, GDStringNode> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDClassNameKeyword, GDIdentifier, GDComma, GDStringNode> TypedForm => _form;

        public GDClassNameAttribute()
        {
            _form = new GDTokensForm<State, GDClassNameKeyword, GDIdentifier, GDComma, GDStringNode>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.ClassName:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDClassNameKeyword>(c, state);
                    break;
                case State.Identifier:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveIdentifier(c, state);
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
            return new GDClassNameAttribute();
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
                _form.State = State.Completed;
                Identifier = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Identifier))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}