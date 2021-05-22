using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDParameters : GDNode
    {
        List<GDParameter> Parameters { get; } = new List<GDParameter>();

        public override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == ',')
            {
                var parameter = new GDParameter();
                Parameters.Add(parameter);
                state.PushNode(parameter);
            }
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}