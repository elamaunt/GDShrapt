namespace GDShrapt.Reader
{
    public sealed class GDDictionaryKeyValueDeclarationList : GDCommaSeparatedList<GDDictionaryKeyValueDeclaration>,
        ITokenReceiver<GDDictionaryKeyValueDeclaration>
    {
        internal override GDReader ResolveNode()
        {
            var node = new GDDictionaryKeyValueDeclaration();
            ListForm.AddToEnd(node);
            return node;
        }

        internal override bool IsStopChar(char c)
        {
            return c == '}';
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDDictionaryKeyValueDeclarationList();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDDictionaryKeyValueDeclaration>.HandleReceivedToken(GDDictionaryKeyValueDeclaration token)
        {
            ListForm.AddToEnd(token);
        }
    }
}
