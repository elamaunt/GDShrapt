namespace GDShrapt.Reader
{
    public class GDBoolExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);

        public bool Value { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.PopNode();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"{Value}";
        }
    }
}
