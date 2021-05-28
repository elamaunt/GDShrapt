namespace GDSharp.Reader
{
    public class GDCallExression : GDExpression
    {
        public override int Priority => 19;

        public GDExpression CallerExpression { get; set; }

        public GDParametersExpression ParametersExpression { get; set; }


        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (ParametersExpression == null)
            {
                state.PushNode(ParametersExpression = new GDParametersExpression());
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

        /* public override GDExpression CombineLeft(GDExpression expr)
         {
             CallerExpression = expr;
             return base.CombineLeft(expr);
         }*/

        public override string ToString()
        {
            return $"{CallerExpression}({ParametersExpression})";
        }
    }
}