namespace GDShrapt.Reader
{
    public class GDSpace : GDCharSequence
    {
        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return IsSpace(c);
        }

        public static GDSpace operator +(GDSpace one, GDSpace other)
        {
            one.Sequence += other.Sequence;
            return one;
        }
    }
}
