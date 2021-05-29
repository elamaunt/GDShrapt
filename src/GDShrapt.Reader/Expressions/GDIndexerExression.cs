namespace GDShrapt.Reader
{
    public class GDIndexerExression : GDExpression
    {
        private bool _openSquareBracketChecked;

        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Indexer);

        public GDExpression CallerExpression { get; set; }
        public GDExpression InnerExpression { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (CallerExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => CallerExpression = expr));
                state.PassChar(c);
                return;
            }

            if (!_openSquareBracketChecked)
            {
                if (c != '[')
                    return;

                _openSquareBracketChecked = true;
                return;
            }

            if (InnerExpression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => InnerExpression = expr));
                state.PassChar(c);
                return;
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"{CallerExpression}[{InnerExpression}]";
        }
    }
}
