namespace GDShrapt.Reader
{
    public sealed class GDDictionaryKeyValueDeclarationList : GDSeparatedList<GDDictionaryKeyValueDeclaration, GDComma>
    {
        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
