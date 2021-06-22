namespace GDShrapt.Reader
{
    public sealed class GDExportParametersList : GDCommaSeparatedList<GDDataToken>,
        IDataTokenReceiver,
        ITokenReceiver<GDComma>
    {
        internal override bool IsStopChar(char c)
        {
            return !c.IsDataStartCharToken();
        }

        internal override GDReader ResolveNode()
        {
            return new GDDataTokensResolver(this);
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
