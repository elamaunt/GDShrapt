namespace GDShrapt.Reader
{
    // TODO: handle godot number patterns
    public class GDNumber : GDCharSequence
    {
        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return char.IsDigit(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}