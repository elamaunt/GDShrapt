namespace GDShrapt.Reader
{
    /// <summary>
    /// Special container for code manipulation
    /// </summary>
    public sealed class GDTokensContainer : GDNode
    {
        public GDTokensListForm<GDSyntaxToken> ListForm { get; }
        public override GDTokensForm Form => ListForm;

        public GDTokensContainer(params GDSyntaxToken[] tokens)
        {
            ListForm = new GDTokensListForm<GDSyntaxToken>(this);

            for (int i = 0; i < tokens.Length; i++)
                ListForm.AddToEnd(tokens[i]);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDTokensContainer();
        }

        internal override void Visit(IGDVisitor visitor)
        {
           // visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
           // visitor.Left(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new GDInvalidStateException();
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            throw new GDInvalidStateException();
        }
    }
}
