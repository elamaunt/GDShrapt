namespace GDShrapt.Reader
{
    public sealed class GDExportParametersList : GDCommaSeparatedList<GDDataToken>,
        ITokenReceiver<GDDataToken>
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
            ListForm.AddToEnd(new GDNewLine());
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDExportParametersList();
        }
        void ITokenReceiver<GDDataToken>.HandleReceivedToken(GDDataToken token)
        {
            ListForm.AddToEnd(token);
        }
    }
}
