namespace GDShrapt.Reader
{
    public sealed class GDDictionaryKeyValueDeclarationList : GDCommaSeparatedList<GDDictionaryKeyValueDeclaration>,
        ITokenReceiver<GDDictionaryKeyValueDeclaration>,
        ITokenReceiver<GDComma>
    {
        internal override GDReader ResolveNode()
        {
            var node = new GDDictionaryKeyValueDeclaration();
            ListForm.Add(node);
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
            ListForm.Add(token);
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            ListForm.Add(token);
        }
    }
}
