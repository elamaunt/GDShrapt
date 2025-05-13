namespace GDShrapt.Reader
{
    public class GDStringTypeNode : GDTypeNode,
        ITokenOrSkipReceiver<GDStringNode>
    {
        public override bool IsArray => false;
        public override bool IsDictionary => false;

        public GDStringNode Path
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public enum State
        {
            Path,
            Completed
        }

        readonly GDTokensForm<State, GDStringNode> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDStringNode> TypedForm => _form;

        public GDStringTypeNode()
        {
            _form = new GDTokensForm<State, GDStringNode>(this);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDSingleTypeNode();
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_form.State == State.Path)
            {
                if (!this.ResolveSpaceToken(c, state))
                    this.ResolveString(c, state);
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

        void ITokenReceiver<GDStringNode>.HandleReceivedToken(GDStringNode token)
        {
            if (_form.State == State.Path)
            {
                _form.State = State.Completed;
                Path = token;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenSkipReceiver<GDStringNode>.HandleReceivedTokenSkip()
        {
            if (_form.State == State.Path)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        public override string BuildName()
        {
            return $"{Path}";
        }
    }
}
