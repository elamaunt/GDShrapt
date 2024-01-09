namespace GDShrapt.Reader
{
    public class GDSubTypeNode : GDTypeNode, 
        ITokenOrSkipReceiver<GDPoint>,
        ITokenOrSkipReceiver<GDTypeNode>,
        ITokenOrSkipReceiver<GDType>
    {
        public override bool IsArray => false;
        public override GDTypeNode SubType => null;
        public GDTypeNode OverType
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDPoint Point
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDType Type
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        public enum State
        {
            OverType,
            Point,
            Type,
            Completed
        }

        readonly GDTokensForm<State, GDTypeNode, GDPoint, GDType> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDTypeNode, GDPoint, GDType> TypedForm => _form;

        public GDSubTypeNode()
        {
            _form = new GDTokensForm<State, GDTypeNode, GDPoint, GDType>(this);
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
                case State.OverType:
                    if (!this.ResolveSpaceToken(c, state))
                        ((ITokenOrSkipReceiver<GDTypeNode>)this).ResolveType(c, state);
                    break;
                case State.Point:
                    if (!this.ResolveSpaceToken(c, state))
                        this.ResolvePoint(c, state);
                    break;
                case State.Type:
                    if (!this.ResolveSpaceToken(c, state))
                        ((ITokenOrSkipReceiver<GDType>)this).ResolveType(c, state);
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


        void ITokenReceiver<GDTypeNode>.HandleReceivedToken(GDTypeNode token)
        {
            if (_form.State == State.OverType)
            {
                _form.State = State.Point;
                OverType = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDTypeNode>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.OverType)
            {
                _form.State = State.Point;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDPoint>.HandleReceivedToken(GDPoint token)
        {
            if (_form.State == State.Point)
            {
                _form.State = State.Type;
                Point = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDPoint>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Point)
            {
                _form.State = State.Type;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDType>.HandleReceivedToken(GDType token)
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Completed;
                Type = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDType>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Type)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        public override string BuildName()
        {
            return $"{OverType?.BuildName()}.{Type?.ToString()}";
        }
    }
}
