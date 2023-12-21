namespace GDShrapt.Reader
{
    public class GDSingleTypeNode : GDTypeNode,
        ITokenOrSkipReceiver<GDType>
    {
        public override GDTypeNode SubType => null;
        public override bool IsArray => Type?.IsArray ?? false;
       
        public GDType Type
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            Type,
            Completed
        }

        readonly GDTokensForm<State, GDType> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDType> TypedForm => _form;

        public GDSingleTypeNode()
        {
            _form = new GDTokensForm<State, GDType>(this);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDSingleTypeNode();
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.Type)
            {
                if (!this.ResolveSpaceToken(c, state))
                    this.ResolveType(c, state);
                return;
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _form.State = State.Completed;
            state.PopAndPassNewLine();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
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
            return $"{Type}";
        }
    }
}
