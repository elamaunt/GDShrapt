using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public class GDParametersExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.Parameters);
        public List<GDExpression> Parameters { get; } = new List<GDExpression>();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == ',')
            {
                state.PushNode(new GDExpressionResolver(expr => Parameters.Add(expr)));
                return;
            }

            if (c == ')')
            {
                state.PopNode();
                return;
            }

            state.PushNode(new GDExpressionResolver(expr => Parameters.Add(expr)));
            state.HandleChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            // Ignore
            // TODO: if needs handling
        }

        public override string ToString()
        {
            return $"({string.Join(",", Parameters.Select(x=> x.ToString()))})";
        }
    }
}