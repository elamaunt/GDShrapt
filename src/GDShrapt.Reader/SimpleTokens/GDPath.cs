namespace GDShrapt.Reader
{
    public sealed class GDPath : GDCharSequence
    {
        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return !IsSpace(c);
        }
    }
}
