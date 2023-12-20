namespace GDShrapt.Reader
{
    public class GDClassMemberAttributeDeclaration : GDClassMember, 
        ITokenOrSkipReceiver<GDAttribute>
    {
        public GDAttribute Attribute
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            Attribute,
            Completed
        }

        readonly GDTokensForm<State, GDAttribute> _form;
        readonly bool _parseWithoutBrackets;

        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDAttribute> TypedForm => _form;

        internal GDClassMemberAttributeDeclaration(int intendation, bool parseWithoutBrackets)
           : base(intendation)
        {
            _form = new GDTokensForm<State, GDAttribute>(this);
            _parseWithoutBrackets = parseWithoutBrackets;
        }

        public GDClassMemberAttributeDeclaration()
        {
            _form = new GDTokensForm<State, GDAttribute>(this);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDClassMemberAttributeDeclaration();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            switch (_form.State)
            {
                case State.Attribute:
                    if (IsSpace(c))
                    {
                        _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                        state.PassChar(c);
                        return;
                    }

                    Attribute = state.PushAndPass(new GDAttribute(_parseWithoutBrackets), c);
                    _form.State = State.Completed;
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

        void ITokenReceiver<GDAttribute>.HandleReceivedToken(GDAttribute token)
        {
            if (_form.State == State.Attribute)
            {
                _form.State = State.Completed;
                Attribute = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDAttribute>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Attribute)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
