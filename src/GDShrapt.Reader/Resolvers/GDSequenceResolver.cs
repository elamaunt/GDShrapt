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
                    OnMatch();
                }
                return;
            }

            state.Pop();
            OnFail();

            for (int i = 0; i < Index - 1; i++)
                state.PassChar(s[i]);

            state.PassChar(c);
        }

        protected abstract void OnFail();
        protected abstract void OnMatch();

        internal override void HandleNewLineChar(GDReadingState state)
        {
            HandleChar('\n', state);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            state.Pop();
            OnFail();

            var s = Sequence;

            for (int i = 0; i < Index - 1; i++)
                state.PassChar(s[i]);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            state.Pop();
            OnFail();

            var s = Sequence;

            for (int i = 0; i < Index - 1; i++)
                state.PassChar(s[i]);
        }
    }
}