using System.Text;

namespace GDScriptConverter
{
    public abstract class GDCharSequence : GDNode
    {
        protected StringBuilder SequenceBuilder { get; set; } = new StringBuilder();

        public bool IsCompleted { get; private set; }
        public string Sequence { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            if (CanAppendChar(c, state))
            {
                SequenceBuilder.Append(c);
                return;
            }

            CompleteSequence(state);
            state.HandleChar(c);
        }

        protected void Append(char c)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            SequenceBuilder.Append(c);
        }

        protected void Append(string str)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            SequenceBuilder.Append(str);
        }

        protected virtual void CompleteSequence(GDReadingState state)
        {
            IsCompleted = true;
            Sequence = SequenceBuilder.ToString();
            SequenceBuilder = null;
            state.PopNode();
        }

        protected internal void ResetSequence()
        {
            IsCompleted = false;
            Sequence = null;
            SequenceBuilder = new StringBuilder();
        }

        protected abstract bool CanAppendChar(char c, GDReadingState state);
    }
}
