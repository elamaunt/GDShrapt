namespace GDShrapt.Reader
{
    public sealed class GDExportParametersList : GDSeparatedList<GDDataToken, GDComma>,
        IDataTokenReceiver,
        ITokenReceiver<GDComma>
    {
        internal override void HandleChar(char c, GDReadingState state)
        {
            if (this.ResolveStyleToken(c, state))
                return;

            if (this.ResolveInvalidToken(c, state, x => (x.IsDataStartCharToken() && !IsExpressionStopChar(x)) || x == ')' || x == ',' || x.IsSpace()))
                return;

            if (IsExpressionStopChar(c))
            {
                if (c == ')')
                {
                    state.PopAndPass(c);
                    return;
                }

                this.ResolveComma(c, state);
            }
            else
            {
                this.ResolveDataToken(c, state);
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            ListForm.Add(new GDNewLine());
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            ListForm.Add(token);
        }

        void IDataTokenReceiver.HandleReceivedToken(GDDataToken token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDComma>.HandleReceivedTokenSkip()
        {
        }

        void IDataTokenReceiver.HandleReceivedTokenSkip()
        {
        }
    }
}
