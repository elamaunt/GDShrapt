namespace GDShrapt.Reader
{
    public sealed class GDBracketExpression : GDExpression, IExpressionsReceiver
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Brackets);

        internal GDOpenBracket OpenBracket { get; set; }
        public GDExpression InnerExpression { get; set; }
        internal GDCloseBracket CloseBracket { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            /* if (IsSpace(c))
                 return;

             if (!_expressionChecked && InnerExpression == null)
             {
                 _expressionChecked = true;
                 state.Push(new GDExpressionResolver(this));
                 state.PassChar(c);
                 return;
             }

             state.Pop();

             if (c != ')')
                 state.PassChar(c);*/
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }
    }
}