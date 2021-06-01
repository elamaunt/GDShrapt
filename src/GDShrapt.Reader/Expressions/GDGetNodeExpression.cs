namespace GDShrapt.Reader
{
    public class GDGetNodeExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.GetNode);

        public string Path { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {

        }

        internal override void HandleLineFinish(GDReadingState state)
        {

        }
    }
}
