using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDParametersExpression : GDExpression
    {
        bool _parametersChecked;

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

            if (!_parametersChecked)
            {
                _parametersChecked = true;
                state.PushNode(new GDExpressionResolver(expr => Parameters.Add(expr)));
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
            if (Parameters.Count == 0)
                return "";

            return $"{string.Join(", ", Parameters.Select(x=> x.ToString()))}";
        }
    }
}