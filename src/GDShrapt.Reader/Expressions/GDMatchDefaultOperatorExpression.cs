namespace GDShrapt.Reader
{
    public class GDMatchDefaultOperatorExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.DefaultOperator);

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
            return "_";
        }
    }
}
