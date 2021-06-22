namespace GDShrapt.Reader
{
    internal abstract class GDSequenceResolver : GDResolver
    {
        public int Index { get; private set; }

        public abstract string Sequence { get; }

        public GDSequenceResolver(IStyleTokensReceiver owner)
            : base(owner)
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            var s = Sequence;

            if (s[Index++] == c)
            {
                if (Index == s.Length)
                {
                    state.Pop();
                    OnMatch(state);
                }
                return;
            }

            state.Pop();
            OnFail(state);

            for (int i = 0; i < Index - 1; i++)
                state.PassChar(s[i]);

            state.PassChar(c);
        }

        protected abstract void OnFail(GDReadingState state);
        protected abstract void OnMatch(GDReadingState state);

        internal override void HandleNewLineChar(GDReadingState state)
        {
            HandleChar('\n', state);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            state.Pop();
            OnFail(state);

            var s = Sequence;

            for (int i = 0; i < Index - 1; i++)
                state.PassChar(s[i]);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            state.Pop();
            OnFail(state);

            var s = Sequence;

            for (int i = 0; i < Index - 1; i++)
                state.PassChar(s[i]);
        }
    }
}