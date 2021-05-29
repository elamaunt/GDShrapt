using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public class GDParametersDeclaration : GDNode
    {
        List<GDParameterDeclaration> Parameters { get; } = new List<GDParameterDeclaration>();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c) || c == '(')
                return;

            if (c == ',')
            {
                var parameter = new GDParameterDeclaration();
                Parameters.Add(parameter);
                state.PushNode(parameter);
                return;
            }

            if (c == ')')
            {
                state.PopNode();
                return;
            }

            {
                var parameter = new GDParameterDeclaration();
                Parameters.Add(parameter);
                state.PushNode(parameter);
                state.PassChar(c);
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"({string.Join(", ", Parameters.Select(x => x.ToString()))})";
        }
    }
}