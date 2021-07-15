namespace GDShrapt.Reader
{
    public sealed class GDPathList : GDSeparatedList<GDIdentifier, GDRightSlash>,
        ITokenReceiver<GDSpace>,
        ITokenOrSkipReceiver<GDIdentifier>,
        ITokenOrSkipReceiver<GDRightSlash>
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
                this.ResolveIdentifier(c, state);
            else
                this.ResolveRightSlash(c, state);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _ended = true;
            state.PopAndPassNewLine();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDPathList();
        }

        void ITokenReceiver<GDIdentifier>.HandleReceivedToken(GDIdentifier token)
        {
            _switch = !_switch;
            ListForm.AddToEnd(token);
        }

        void ITokenReceiver<GDRightSlash>.HandleReceivedToken(GDRightSlash token)
        {
            _switch = !_switch;
            ListForm.AddToEnd(token);
        }
        void ITokenSkipReceiver<GDIdentifier>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }

        void ITokenSkipReceiver<GDRightSlash>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }

        void ITokenReceiver<GDSpace>.HandleReceivedToken(GDSpace token)
        {
            ListForm.AddToEnd(token);
        }
    }
}
