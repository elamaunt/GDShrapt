namespace GDShrapt.Reader
{
    public sealed class GDEnumValuesList : GDSeparatedList<GDEnumValueDeclaration, GDNewLine>
    {
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
