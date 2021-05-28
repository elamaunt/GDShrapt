namespace GDSharp.Reader
{
    public class GDNumberExpression : GDExpression
    {
        public GDNumber Number { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Number == null)
            {
                state.PushNode(Number = new GDNumber());
                state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }
        public override int Priority => throw new System.NotImplementedException();
    }
}
