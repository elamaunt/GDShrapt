namespace GDShrapt.Reader
{
    internal abstract class GDIntendedSequenceResolver : GDIntendedResolver
    {
        public int Index { get; private set; }
        public abstract string Sequence { get; }
        public bool PopStateOnMatch { get; }

        public GDIntendedSequenceResolver(IIntendedTokenReceiver owner, int lineIntendation, bool popStateOnMatch = true)
            : base(owner, lineIntendation)
        {
            PopStateOnMatch = popStateOnMatch;
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            var s = Sequence;

            if (s[Index++] == c)
            {
                if (Index == s.Length)
                {
                    if (PopStateOnMatch)
                        state.Pop();

                    OnMatch(state);

                    if (!PopStateOnMatch)
                    {
                        Index = 0;
                        ResetIntendation();
                    }
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

        protected override void OnIntendationThresholdMet(GDReadingState state)
        {
            base.OnIntendationThresholdMet(state);
            OnFail(state);
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

        internal override void HandleSharpCharAfterIntendation(GDReadingState state)
        {
            var s = Sequence;
            state.Pop();
            OnFail(state);

            PassIntendationSequence(state);

            for (int i = 0; i < Index - 1; i++)
                state.PassChar(s[i]);

            state.PassSharpChar();
        }

        internal override void HandleLeftSlashCharAfterIntendation(GDReadingState state)
        {
            var s = Sequence;
            state.Pop();
            OnFail(state);

            PassIntendationSequence(state);

            for (int i = 0; i < Index - 1; i++)
                state.PassChar(s[i]);

            state.PassLeftSlashChar();
        }
    }
}