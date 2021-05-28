namespace GDSharp.Reader
{
    public class GDArrayInitializerExpression : GDExpression
    {
        public override int Priority => throw new System.NotImplementedException();

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (c == ']')
            {
            }
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }
    }
}
