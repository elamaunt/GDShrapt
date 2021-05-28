namespace GDShrapt.Reader
{
    public class GDNumberExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);
        public GDNumber Number { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Number == null)
            {
                state.PushNode(Number = new GDNumber());
                state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }
    }
}
