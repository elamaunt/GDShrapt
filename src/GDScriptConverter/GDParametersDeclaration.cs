using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDParametersDeclaration : GDNode
    {
        List<GDParameterDeclaration> Parameters { get; } = new List<GDParameterDeclaration>();

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == ',')
            {
                var parameter = new GDParameterDeclaration();
                Parameters.Add(parameter);
                state.PushNode(parameter);
            }
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}