namespace GDShrapt.Reader
{
    public sealed class GDExportDeclaration : GDNode
    {
        internal GDExportKeyword ExportKeyword
        {
            get;
            set;
        }

        internal GDOpenBracket OpenBracket
        {
            get;
            set;
        }

        public GDExportParametersList Parameters { get; }

        internal GDOpenBracket CloseBracket
        {
            get;
            set;
        }

        internal override GDTokensForm Form => throw new System.NotImplementedException();

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
