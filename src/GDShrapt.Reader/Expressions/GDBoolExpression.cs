namespace GDShrapt.Reader
{
    public sealed class GDBoolExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);

        public bool Value { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        public override string ToString()
        {
            return $"{Value}";
        }
    }
}
