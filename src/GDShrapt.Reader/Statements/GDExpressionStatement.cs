﻿namespace GDShrapt.Reader
{
    public class GDExpressionStatement : GDStatement
    {
        public GDExpression Expression { get; set; }


        internal override void HandleChar(char c, GDReadingState state)
        {
            if (Expression == null)
            {
                state.PushNode(new GDExpressionResolver(expr => Expression = expr));
                state.HandleChar(c);
                return;
            }

            state.PopNode();
            state.HandleChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }
    }
}