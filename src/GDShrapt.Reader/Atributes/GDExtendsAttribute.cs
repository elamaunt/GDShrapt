namespace GDShrapt.Reader
{
    public sealed class GDExtendsAttribute : GDClassAttribute,
        ITokenOrSkipReceiver<GDExtendsKeyword>,
        ITokenOrSkipReceiver<GDTypeNode>
    {
        public GDExtendsKeyword ExtendsKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }
        public GDTypeNode Type
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

        readonly GDTokensForm<State, GDExtendsKeyword, GDTypeNode> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDExtendsKeyword, GDTypeNode> TypedForm => _form;
        public GDExtendsAttribute()
        {
            _form = new GDTokensForm<State, GDExtendsKeyword, GDTypeNode>(this);
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
            return new GDExtendsAttribute();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
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

        void ITokenReceiver<GDTypeNode>.HandleReceivedToken(GDTypeNode token)
        {
            if (_form.IsOrLowerState(State.Type))
            {
                _form.State = State.Completed;
                Type = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDTypeNode>.HandleReceivedTokenSkip()
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