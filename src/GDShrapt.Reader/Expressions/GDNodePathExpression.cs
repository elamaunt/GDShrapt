namespace GDShrapt.Reader
{
    public sealed class GDNodePathExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.NodePath);

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
