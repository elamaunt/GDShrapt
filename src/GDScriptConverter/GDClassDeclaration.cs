using System;
using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDClassDeclaration : GDNode
    {
        public List<GDClassMember> Members { get; } = new List<GDClassMember>();

        public override void HandleChar(char c, GDReadingState state)
        {
            state.PushNode(new GDClassMemberResolver(this));
            state.HandleChar(c);
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            // Nothing
        }
    }
}