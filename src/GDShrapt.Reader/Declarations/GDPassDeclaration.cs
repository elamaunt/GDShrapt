namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents a 'pass' statement at class level (used for empty class bodies).
    /// </summary>
    public sealed class GDPassDeclaration : GDClassMember,
        ITokenOrSkipReceiver<GDPassKeyword>
    {
        public GDPassKeyword PassKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            Pass,
            Completed
        }

        readonly GDTokensForm<State, GDPassKeyword> _form;
        public override GDTokensForm Form => _form;

        public GDTokensForm<State, GDPassKeyword> TypedForm => _form;

        internal GDPassDeclaration(int intendation)
            : base(intendation)
        {
            _form = new GDTokensForm<State, GDPassKeyword>(this);
        }

        public GDPassDeclaration()
        {
            _form = new GDTokensForm<State, GDPassKeyword>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Pass:
                    this.ResolveKeyword<GDPassKeyword>(c, state);
                    break;
                default:
                    state.PopAndPass(c);
                    break;
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _form.State = State.Completed;
            state.PopAndPassNewLine();
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            _form.State = State.Completed;
            state.PopAndPassCarriageReturnChar();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDPassDeclaration();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDPassKeyword>.HandleReceivedToken(GDPassKeyword token)
        {
            if (_form.IsOrLowerState(State.Pass))
            {
                _form.State = State.Completed;
                PassKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDPassKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Pass))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
