using System;

namespace GDShrapt.Reader
{
    internal class GDStaticKeywordResolver : GDNode
    {
        readonly string _keyword;
        readonly Action<bool> _handler;

        int _index;

        public GDStaticKeywordResolver(string keyword, Action<bool> handler)
        {
            _keyword = keyword;
            _handler = handler;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            // Compares the keyword chars one by one
            if (c == _keyword[_index])
            {
                if ((++_index) == _keyword.Length)
                    Complete(true, state);
            }
            else
            {
                Complete(false, state);
                state.PopNode();
                state.PassChar(c);
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            Complete(false, state);
            state.PassLineFinish();
        }

        private void Complete(bool result, GDReadingState state)
        {
            _handler(result);
            state.PopNode();
        }
    }
}
