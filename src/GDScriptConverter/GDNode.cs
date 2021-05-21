using System;

namespace GDScriptConverter
{
    public abstract class GDNode
    {
        public abstract void HandleChar(char c, GDReadingState state);
        public abstract void HandleLineFinish(GDReadingState state);

        public bool IsSpace(char c) => c == ' ' || c == '\t';
    }
}