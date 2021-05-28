namespace GDShrapt.Reader
{
    public class GDBracketExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Brackets);

        public GDExpression InnerExpression { get; set; }
        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"({InnerExpression})";
        }
    }
}