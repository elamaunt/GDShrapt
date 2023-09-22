namespace GDShrapt.Reader
{
    public class GDSingleTypeNode : GDTypeNode,
        ITokenOrSkipReceiver<GDType>
    {
        public override GDTypeNode SubType => null;
        public override bool IsArray => false;
       
        public enum State
        {
            Type,
            Completed
        }

        readonly GDTokensForm<State, GDTypeNode> _form;
        public override GDTokensForm Form => _form;
        public GDTokensForm<State, GDTypeNode> TypedForm => _form;

        public GDSingleTypeNode()
        {
            _form = new GDTokensForm<State, GDTypeNode>(this);
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

        void ITokenReceiver<GDType>.HandleReceivedToken(GDType token)
        {

        }

        void ITokenSkipReceiver<GDType>.HandleReceivedTokenSkip()
        {

        }
    }
}
