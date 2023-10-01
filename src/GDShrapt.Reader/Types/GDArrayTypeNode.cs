namespace GDShrapt.Reader.Types
{
    public class GDArrayTypeNode : GDTypeNode, 
        ITokenOrSkipReceiver<GDArrayKeyword>,
        ITokenOrSkipReceiver<GDSquareOpenBracket>,
        ITokenOrSkipReceiver<GDSquareCloseBracket>,
        ITokenOrSkipReceiver<GDTypeNode>
    {
        public override bool IsArray => true;

        public GDArrayKeyword ArrayKeyword
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDSquareOpenBracket SquareOpenBracket
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public override GDTypeNode SubType => InnerType;
        public GDTypeNode InnerType
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public GDSquareCloseBracket SquareCloseBracket
        {
            get => _form.Token3;
            set => _form.Token3 = value;
        }

        public enum State
        {
            Array,
            SquareOpenBracket,
            InnerType,
            SquareCloseBracket,
            Completed
        }

        readonly GDTokensForm<State, GDArrayKeyword, GDSquareOpenBracket, GDTypeNode, GDSquareCloseBracket> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDArrayKeyword, GDSquareOpenBracket, GDTypeNode, GDSquareCloseBracket> TypedForm => _form;

        public GDArrayTypeNode()
        {
            _form = new GDTokensForm<State, GDArrayKeyword, GDSquareOpenBracket, GDTypeNode, GDSquareCloseBracket>(this);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDArrayTypeNode();
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
                case State.Array:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveKeyword<GDArrayKeyword>(c, state);
                    break;
                case State.SquareOpenBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveSquareOpenBracket(c, state);
                    break;
                case State.InnerType:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveType(c, state);
                    break;
                case State.SquareCloseBracket:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolveSquareCloseBracket(c, state);
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

        void ITokenReceiver<GDArrayKeyword>.HandleReceivedToken(GDArrayKeyword token)
        {
            if (_form.State == State.Array)
            {
                _form.State = State.SquareOpenBracket;
                ArrayKeyword = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDArrayKeyword>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Array)
            {
                _form.State = State.SquareOpenBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDSquareOpenBracket>.HandleReceivedToken(GDSquareOpenBracket token)
        {
            if (_form.IsOrLowerState(State.SquareOpenBracket))
            {
                _form.State = State.InnerType;
                SquareOpenBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSquareOpenBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.SquareOpenBracket))
            {
                _form.State = State.InnerType;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDTypeNode>.HandleReceivedToken(GDTypeNode token)
        {
            if (_form.State == State.InnerType)
            {
                _form.State = State.SquareCloseBracket;
                InnerType = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDTypeNode>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.InnerType)
            {
                _form.State = State.SquareCloseBracket;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDSquareCloseBracket>.HandleReceivedToken(GDSquareCloseBracket token)
        {
            if (_form.IsOrLowerState(State.SquareCloseBracket))
            {
                _form.State = State.Completed;
                SquareCloseBracket = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDSquareCloseBracket>.HandleReceivedTokenSkip()
        {
            if (_form.IsOrLowerState(State.SquareCloseBracket))
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }
    }
}
