using System.Text;

namespace GDScriptConverter
{
    public abstract class GDCharSequenceNode : GDNode
    {
        StringBuilder _sequenceBuilder = new StringBuilder();

        public bool IsCompleted { get; private set; }
        public string Sequence { get; set; }

        public override void HandleChar(char c, GDReadingState state)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            if (CanAppendChar(c, state))
            {
                _sequenceBuilder.Append(c);
                return;
            }

            CompleteSequence(state);
            state.HandleChar(c);
        }

        protected void Append(char c)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            _sequenceBuilder.Append(c);
        }

        protected void Append(string str)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            _sequenceBuilder.Append(str);
        }

        protected virtual void CompleteSequence(GDReadingState state)
        {
            IsCompleted = true;
            Sequence = _sequenceBuilder.ToString();
            _sequenceBuilder = null;
            state.PopNode();
        }

        protected abstract bool CanAppendChar(char c, GDReadingState state);
    }
}
