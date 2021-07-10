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
                ListForm.Add(tokens[i]);
        }

        public override GDNode CreateEmptyInstance()
        {
            return new GDTokensContainer();
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
