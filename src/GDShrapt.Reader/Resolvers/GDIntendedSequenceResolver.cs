namespace GDShrapt.Reader
{
    internal abstract class GDIntendedSequenceResolver : GDIntendedResolver
    {
        public int Index { get; private set; }
        public abstract string Sequence { get; }

        public GDIntendedSequenceResolver(IIntendationReceiver owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
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

            PassIntendationSequence(state);

            for (int i = 0; i < Index - 1; i++)
                state.PassChar(s[i]);

            state.PassChar(c);
        }

        protected abstract void OnFail(GDReadingState state);
        protected abstract void OnMatch(GDReadingState state);

        internal override void HandleNewLineAfterIntendation(GDReadingState state)
        {
            HandleChar('\n', state);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            var s = Sequence;
            state.Pop();
            OnFail(state);

            PassIntendationSequence(state);

            for (int i = 0; i < Index - 1; i++)
                state.PassChar(s[i]);
        }
    }
}