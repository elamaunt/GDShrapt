namespace GDShrapt.Reader
{
    public sealed class GDCallExression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Call);

        public GDExpression CallerExpression { get; set; }

        public GDParametersExpression ParametersExpression { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (ParametersExpression == null)
            {
                state.Push(ParametersExpression = new GDParametersExpression());
                state.PassChar(c);
                return;
            }

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
            return $"{CallerExpression}({ParametersExpression})";
        }
    }
}