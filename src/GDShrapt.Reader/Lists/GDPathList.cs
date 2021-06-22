namespace GDShrapt.Reader
{
    public sealed class GDPathList : GDSeparatedList<GDIdentifier, GDRightSlash>,
        IIdentifierReceiver,
        ITokenReceiver<GDRightSlash>
    {
        bool _switch;
        bool _ended;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveStyleToken(c, state))
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

        void IIdentifierReceiver.HandleReceivedToken(GDIdentifier token)
        {
            _switch = !_switch;
            ListForm.Add(token);
        }

        void ITokenReceiver<GDRightSlash>.HandleReceivedToken(GDRightSlash token)
        {
            _switch = !_switch;
            ListForm.Add(token);
        }
        void IIdentifierReceiver.HandleReceivedIdentifierSkip()
        {
            _ended = true;
        }

        void ITokenReceiver<GDRightSlash>.HandleReceivedTokenSkip()
        {
            _ended = true;
        }
    }
}
