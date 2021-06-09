namespace GDShrapt.Reader
{
    public class GDSpace : GDCharSequence
    {
        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return IsSpace(c);
        }
    }
}
