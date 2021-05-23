﻿namespace GDScriptConverter
{
    public class GDComment : GDCharSequenceNode
    {
        public GDComment()
        {
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.LineFinished();
        }

        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return true;
        }

        protected internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }
    }
}