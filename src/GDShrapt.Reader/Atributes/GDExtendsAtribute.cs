namespace GDShrapt.Reader
{
    public sealed class GDExtendsAtribute : GDClassAtribute,
        ITokenOrSkipReceiver<GDExtendsKeyword>,
        ITokenOrSkipReceiver<GDType>
    {
        public GDExtendsKeyword ExtendsKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDType Type
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }
        public enum State
        {
            Extends,
            Type,
            Completed
        }

        readonly GDTokensForm<State, GDExtendsKeyword, GDType> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDExtendsKeyword, GDType> TypedForm => _form;
        public GDExtendsAtribute()
        {
            _form = new GDTokensForm<State, GDExtendsKeyword, GDType>(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            switch (_form.State)
            {
                case State.Extends:
                    this.ResolveKeyword<GDExtendsKeyword>(c, state);
                    break;
                case State.Type:
                    this.ResolveType(c, state);
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
            return new GDExtendsAtribute();
        }

        void ITokenReceiver<GDExtendsKeyword>.HandleReceivedToken(GDExtendsKeyword token)
        {
            if (_form.IsOrLowerState(State.Extends))
            {
                _form.State = State.Type;
                ExtendsKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDExtendsKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Extends))
            {
                _form.State = State.Type;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDType>.HandleReceivedToken(GDType token)
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Completed;
                Type = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDType>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}