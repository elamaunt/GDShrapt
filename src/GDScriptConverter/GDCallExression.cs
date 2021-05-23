namespace GDScriptConverter
{
    public class GDCallExression : GDExpression
    {
        public GDExpression CallerExpression { get; set; }

        public GDParametersExpression ParametersExpression { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (ParametersExpression == null)
            {
                state.PushNode(ParametersExpression = new GDParametersExpression());
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