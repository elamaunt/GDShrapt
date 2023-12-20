namespace GDShrapt.Reader
{
    public class GDStringPartsList : GDSeparatedList<GDStringPart, GDMultiLineSplitToken>, 
        ITokenOrSkipReceiver<GDStringPart>,
        ITokenOrSkipReceiver<GDMultiLineSplitToken>
    {
        bool _ended;
        readonly GDStringBoundingChar _bounder;

        public GDStringPartsList()
        {
        }

        internal GDStringPartsList(GDStringBoundingChar bounder)
        {
            _bounder = bounder;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (_ended)
            {
                state.PopAndPass(c);
                return;
            }

            this.ResolveStringPart(c, state, _bounder);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _ended = true;
            state.PopAndPassNewLine();
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            _ended = false;
            ListForm.AddToEnd(state.Push(new GDMultiLineSplitToken()));
            state.PassLeftSlashChar();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDStringPartsList();
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        void ITokenReceiver<GDStringPart>.HandleReceivedToken(GDStringPart token)
        {
            ListForm.AddToEnd(token);
        }

        void ITokenSkipReceiver<GDStringPart>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }

        void ITokenReceiver<GDMultiLineSplitToken>.HandleReceivedToken(GDMultiLineSplitToken token)
        {
            ListForm.AddToEnd(token);
        }

        void ITokenSkipReceiver<GDMultiLineSplitToken>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }
    }
}
