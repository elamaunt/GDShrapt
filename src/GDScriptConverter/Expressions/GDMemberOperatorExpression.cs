namespace GDScriptConverter
{
    public class GDMemberOperatorExpression : GDExpression
    {
        public GDExpression CallerExpression { get; set; }
        public GDIdentifier Identifier { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (CallerExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => CallerExpression = expr));
                state.HandleChar(c);
                return;
            }

            if (Identifier == null)
            {
                state.PushNode(Identifier = new GDIdentifier());

                if (c != '.')
                    state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.LineFinished();
        }

       /* public override GDExpression CombineLeft(GDExpression expr)
        {
            CallerExpression = expr;
            return base.CombineLeft(expr);
        }*/
    }
}