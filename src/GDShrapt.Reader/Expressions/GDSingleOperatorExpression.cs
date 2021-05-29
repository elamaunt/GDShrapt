namespace GDShrapt.Reader
{
    public class GDSingleOperatorExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperatorPriority(OperatorType);
        public GDSingleOperatorType OperatorType { get; set; }
        public GDExpression TargetExpression { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (OperatorType == GDSingleOperatorType.Null)
            {
                state.PushNode(new GDSingleOperatorResolver((op, comment) =>
                {
                    OperatorType = op;
                    EndLineComment = comment;
                }));
                state.PassChar(c);
                return;
            }

            if (TargetExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => TargetExpression = expr));
                state.PassChar(c);
                return;
            }

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
            return $"{OperatorType.Print()}{TargetExpression})";
        }
    }
}

