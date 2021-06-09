namespace GDShrapt.Reader
{
    public sealed class GDContinueExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Continue);

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"continue";
        }
    }
}
