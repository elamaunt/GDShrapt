using System;
using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDClass : GDNode
    {
        public List<GDStatement> Statements { get; } = new List<GDStatement>();

        public override void HandleChar(char c, GDReadingState state)
        {
            state.PushNode(new GDClassStatementResolver(this));
            state.HandleChar(c);
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            // Nothing
        }
    }
}