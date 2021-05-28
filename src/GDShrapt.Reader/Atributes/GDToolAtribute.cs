namespace GDShrapt.Reader
{
    public class GDToolAtribute : GDClassMember
    {
        internal override void HandleChar(char c, GDReadingState state)
        {
            state.PopNode();
            state.HandleChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
        }

        public override string ToString()
        {
            return $"tool";
        }
    }
}