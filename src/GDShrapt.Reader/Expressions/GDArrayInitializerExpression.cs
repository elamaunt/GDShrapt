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
                state.Pop();
                return;
            }

            if (c == ',')
            {
                state.Push(new GDExpressionResolver(this));
                return;
            }

            state.Push(new GDExpressionResolver(this));
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", Values.Select(x => x.ToString()))}]";
        }
    }
}
