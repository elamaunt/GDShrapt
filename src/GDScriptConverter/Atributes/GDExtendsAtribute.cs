namespace GDScriptConverter
{
    public class GDExtendsAtribute : GDClassMember
    {
        public GDType Type { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Type == null)
            {
                state.PushNode(Type = new GDType());
                state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.LineFinished();
        }
    }
}