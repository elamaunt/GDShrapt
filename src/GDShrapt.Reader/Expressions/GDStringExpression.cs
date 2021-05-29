namespace GDShrapt.Reader
{
    public class GDStringExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Literal);
        public GDString String { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (String == null)
            {
                state.PushNode(String = new GDString());
                state.PassChar(c);
                return;
            }

            state.PopNode();

            if (c != '\"')
                state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"{String}";
        }
    }
}