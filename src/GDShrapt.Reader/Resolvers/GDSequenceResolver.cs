using System.Text;

namespace GDShrapt.Reader
{
    internal abstract class GDSequenceResolver : GDResolver
    {
        readonly StringBuilder _sequenceBuilder = new StringBuilder();
        int _index = 0;

        public abstract string Sequence { get; }

        public GDSequenceResolver(IStyleTokensReceiver owner)
            : base(owner)
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            var s = Sequence;

            if (s[_index++] == c)
            {
                if (_index == s.Length)
                {
                    state.Pop();
                    OnMatch();
                }
                return;
            }

            state.Pop();
            OnFail();

            for (int i = 0; i < _index - 1; i++)
                state.PassChar(s[i]);

            state.PassChar(c);
        }

        protected abstract void OnFail();
        protected abstract void OnMatch();

        internal override void HandleLineFinish(GDReadingState state)
        {
            HandleChar('\n', state);
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            HandleChar('#', state);
        }
    }
}