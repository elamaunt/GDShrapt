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
                state.PushNode(ParametersExpression = new GDParametersExpression());
                state.PassChar(c);
                return;
            }

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
            return $"{CallerExpression}({ParametersExpression})";
        }
    }
}