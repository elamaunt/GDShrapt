namespace GDScriptConverter
{
    public class GDStringExpression : GDExpression
    {
        public GDString String { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (String == null)
            {
                state.PushNode(String = new GDString());
                state.HandleChar(c);
                return;
            }

            state.PopNode();

            if (c != '\"')
                state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.LineFinished();
        }
    }
}