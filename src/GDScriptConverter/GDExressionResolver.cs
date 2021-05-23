using System;
using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDExressionResolver : GDNode
    {
        readonly Action<GDExpression> _handler;

        Stack<GDExpression> _expressionsStack = new Stack<GDExpression>();

        public GDExressionResolver(Action<GDExpression> handler)
        {
            _handler = handler;
        }

        public override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (char.IsDigit(c))
            {
                state.PushNode(new GDNumberExpression());
                state.HandleChar(c);
                return;
            }

            if (c=='"')
            {
                state.PushNode(new GDStringExpression());
                return;
            }

            state.PushNode(_expressionsStack.PushAndPeek(new GDIdentifierExpression()));


            if (c == '"')
            {
                
                return;
            }

            // TODO: another expressions
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            throw new NotImplementedException();
        }
    }
}