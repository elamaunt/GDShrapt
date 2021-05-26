namespace GDScriptConverter
{
    public class GDToolAtribute : GDClassMember
    {
        protected internal override void HandleChar(char c, GDReadingState state)
        {
            state.PopNode();
            state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
        }

        public override string ToString()
        {
            return $"tool";
        }
    }
}