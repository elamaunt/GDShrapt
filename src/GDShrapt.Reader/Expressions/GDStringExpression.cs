namespace GDShrapt.Reader
{
    public sealed class GDStringExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);
        public GDString String { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (String == null)
            {
                state.Push(String = new GDString());
                state.PassChar(c);
                return;
            }

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
            return $"{String}";
        }
    }
}