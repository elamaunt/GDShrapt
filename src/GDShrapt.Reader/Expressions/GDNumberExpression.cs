namespace GDShrapt.Reader
{
    public sealed class GDNumberExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);
        public GDNumber Number { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Number == null)
            {
                state.Push(Number = new GDNumber());
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
            return $"{Number}";
        }
    }
}
