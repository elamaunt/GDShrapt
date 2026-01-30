using System.Text;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Basic token type to read any sequences of chars
    /// </summary>
    public abstract class GDCharSequence : GDSimpleSyntaxToken
    {
        StringBuilder _sequenceBuilder = new StringBuilder();

        internal bool IsCompleted { get; private set; }
        public string Sequence { get; protected set; }

        internal int SequenceBuilderLength => _sequenceBuilder.Length;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            if (CanAppendChar(c, state))
            {
                _sequenceBuilder.Append(c);
                return;
            }

            CompleteSequence(state);
            state.PassChar(c);
        }

        internal void Append(char c)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            _sequenceBuilder.Append(c);
        }

        internal void Append(string str)
        {
            if (IsCompleted)
                throw new System.Exception("Char sequence already completed");

            _sequenceBuilder.Append(str);
        }

        internal virtual void CompleteSequence(GDReadingState state)
        {
            Complete();
            state.Pop();
        }

        internal void Complete()
        {
            IsCompleted = true;
            Sequence = _sequenceBuilder.ToString();
            _sequenceBuilder = null;
        }

        internal void ResetSequence()
        {
            IsCompleted = false;
            Sequence = null;
            _sequenceBuilder = new StringBuilder();
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            Complete();
            base.HandleSharpChar(state);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            Complete();
            base.HandleNewLineChar(state);
        }

        internal override void HandleLeftSlashChar(GDReadingState state)
        {
            Complete();
            base.HandleLeftSlashChar(state);
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            Complete();
            base.HandleCarriageReturnChar(state);
        }

        internal override void ForceComplete(GDReadingState state)
        {
            CompleteSequence(state);
        }

        /// <summary>
        /// Checks whether the char should be added to current sequence
        /// </summary>
        /// <param name="c">Char to check</param>
        /// <param name="state">Current reading state</param>
        /// <returns>Should add char to sequence</returns>
        internal abstract bool CanAppendChar(char c, GDReadingState state);

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}
