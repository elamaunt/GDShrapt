using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public class GDArrayInitializerExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.ArrayInitializer);
        public List<GDExpression> Values { get; } = new List<GDExpression>();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (c == ']')
            {
                state.PopNode();
                return;
            }

            if (c == ',')
            {
                state.PushNode(new GDExpressionResolver(expr => Values.Add(expr)));
                return;
            }

            state.PushNode(new GDExpressionResolver(expr => Values.Add(expr)));
            state.HandleChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }
    }
}
