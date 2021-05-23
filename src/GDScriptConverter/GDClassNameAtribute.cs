namespace GDScriptConverter
{
    public class GDClassNameAtribute : GDClassMember
    {
        public GDIdentifier Identifier { get; set; }

        public override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Identifier == null)
            {
                state.PushNode(Identifier = new GDIdentifier());
                state.HandleChar(c);
            }
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
        }
    }
}