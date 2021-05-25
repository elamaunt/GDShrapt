using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDParametersExpression : GDExpression
    {
        public List<GDExpression> Parameters { get; } = new List<GDExpression>();

        protected internal override void HandleChar(char c, GDReadingState state)
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

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            // Ignore
            // TODO: if needs handling
        }
    }
}