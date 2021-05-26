namespace GDScriptConverter
{
    public class GDSingleOperatorExpression : GDExpression
    {
        public GDSingleOperatorType OperatorType { get; set; }
        public GDExpression TargetExpression { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (OperatorType == GDSingleOperatorType.Null)
            {
                state.PushNode(new GDSingleOperatorResolver(op => OperatorType = op));
                state.HandleChar(c);
                return;
            }

            if (TargetExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => TargetExpression = expr));
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
        public override int Priority
        {
            get
            {
                switch (OperatorType)
                {
                    case GDSingleOperatorType.Null: 
                    case GDSingleOperatorType.Unknown: return 20;
                    default:
                        return 16;
                };
            }
        }

        public override string ToString()
        {
            return $"{OperatorType.Print()}{TargetExpression})";
        }
    }
}

