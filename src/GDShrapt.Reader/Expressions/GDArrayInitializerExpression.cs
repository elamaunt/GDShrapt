using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDArrayInitializerExpression : GDExpression
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
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", Values.Select(x => x.ToString()))}]";
        }
    }
}
