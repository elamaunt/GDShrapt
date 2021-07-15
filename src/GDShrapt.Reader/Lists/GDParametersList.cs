namespace GDShrapt.Reader
{
    public sealed class GDParametersList : GDCommaSeparatedList<GDParameterDeclaration>,
        ITokenReceiver<GDParameterDeclaration>
    {
        internal override bool IsStopChar(char c)
        {
            return c == ')';
        }

        internal override GDReader ResolveNode()
        {
            var node = new GDParameterDeclaration();
            ListForm.AddToEnd(node);
            return node;
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDParametersList();
        }

        void ITokenReceiver<GDParameterDeclaration>.HandleReceivedToken(GDParameterDeclaration token)
        {
            ListForm.AddToEnd(token);
        }
    }
}
