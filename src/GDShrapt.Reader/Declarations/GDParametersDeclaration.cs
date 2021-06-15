using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDParametersDeclaration : GDNode
    {
        public List<GDParameterDeclaration> Parameters { get; } = new List<GDParameterDeclaration>();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c) || c == '(')
                return;

            if (c == ',')
            {
                var parameter = new GDParameterDeclaration();
                Parameters.Add(parameter);
                state.Push(parameter);
                return;
            }

            if (c == ')')
            {
                state.Pop();
                return;
            }

            {
                var parameter = new GDParameterDeclaration();
                Parameters.Add(parameter);
                state.Push(parameter);
                state.PassChar(c);
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"({string.Join(", ", Parameters.Select(x => x.ToString()))})";
        }
    }
}