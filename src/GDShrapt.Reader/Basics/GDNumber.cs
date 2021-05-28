namespace GDShrapt.Reader
{
    public class GDNumber : GDCharSequence
    {
        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return char.IsDigit(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}