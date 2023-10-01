namespace GDShrapt.Reader
{
    public class GDLayersList : GDSeparatedList<GDPathSpecifier, GDColon>,
        ITokenReceiver<GDSpace>,
        ITokenOrSkipReceiver<GDColon>,
        ITokenOrSkipReceiver<GDPathSpecifier>
    {
        bool _switch;
        bool _ended;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveSpaceToken(c, state))
                return;

            if (_ended)
            {
                state.PopAndPass(c);
                return;
            }

            if (!_switch)
                this.ResolvePathSpecifier(c, state);
            else
                this.ResolveColon(c, state);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _ended = true;
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDLayersList();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDPathSpecifier>.HandleReceivedToken(GDPathSpecifier token)
        {
            _switch = !_switch;
            ListForm.AddToEnd(token);
        }

        void ITokenReceiver<GDColon>.HandleReceivedToken(GDColon token)
        {
            _switch = !_switch;
            ListForm.AddToEnd(token);
        }

        void ITokenSkipReceiver<GDPathSpecifier>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }

        void ITokenSkipReceiver<GDColon>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }

        void ITokenReceiver<GDSpace>.HandleReceivedToken(GDSpace token)
        {
            ListForm.AddToEnd(token);
        }
    }
}