namespace GDShrapt.Reader
{
    public class GDStringExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);
        public GDString String { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
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

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }
    }
}