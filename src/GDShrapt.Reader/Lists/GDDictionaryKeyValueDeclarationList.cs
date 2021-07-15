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

        void ITokenReceiver<GDDictionaryKeyValueDeclaration>.HandleReceivedToken(GDDictionaryKeyValueDeclaration token)
        {
            ListForm.AddToEnd(token);
        }
    }
}
