using System.Text;

namespace GDShrapt.Reader
{
    public abstract class GDCharSequence : GDNode
    {
        internal StringBuilder SequenceBuilder { get; set; } = new StringBuilder();

        internal bool IsCompleted { get; private set; }
        public string Sequence { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            if (CanAppendChar(c, state))
            {
                SequenceBuilder.Append(c);
                return;
            }

            CompleteSequence(state);
            state.PassChar(c);
        }

        internal void Append(char c)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            SequenceBuilder.Append(c);
        }

        internal void Append(string str)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            SequenceBuilder.Append(str);
        }

        internal virtual void CompleteSequence(GDReadingState state)
        {
            IsCompleted = true;
            Sequence = SequenceBuilder.ToString();
            SequenceBuilder = null;
            state.PopNode();
        }

        internal void ResetSequence()
        {
            IsCompleted = false;
            Sequence = null;
            SequenceBuilder = new StringBuilder();
        }

        internal abstract bool CanAppendChar(char c, GDReadingState state);
    }
}
