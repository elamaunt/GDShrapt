﻿namespace GDScriptConverter
{
    public class GDNumberExpression : GDCharSequenceNode
    {
        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return char.IsDigit(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}