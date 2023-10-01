namespace GDShrapt.Reader
{
    public sealed class GDEnumValuesList : GDCommaSeparatedList<GDEnumValueDeclaration>,
        ITokenReceiver<GDEnumValueDeclaration>
    {
        internal override GDReader ResolveNode()
        {
            var node = new GDEnumValueDeclaration();
            ListForm.AddToEnd(node);
            return node;
        }

        internal override bool IsStopChar(char c)
        {
            return !c.IsIdentifierStartChar();
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDEnumValuesList();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        void ITokenReceiver<GDEnumValueDeclaration>.HandleReceivedToken(GDEnumValueDeclaration token)
        {
            ListForm.AddToEnd(token);
        }
    }
}
