namespace GDShrapt.Reader
{
    public class GDToolAtribute : GDClassMember
    {
        internal override void HandleChar(char c, GDReadingState state)
        {
            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
        }

        public override string ToString()
        {
            return $"tool";
        }
    }
}