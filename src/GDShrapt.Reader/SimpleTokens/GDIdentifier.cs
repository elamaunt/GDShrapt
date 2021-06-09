using System;

namespace GDShrapt.Reader
{
    public class GDIdentifier : GDCharSequence
    {
        public bool IsPi => string.Equals(Sequence, "PI", StringComparison.Ordinal);
        public bool IsTau => string.Equals(Sequence, "TAU", StringComparison.Ordinal);
        public bool IsInfinity => string.Equals(Sequence, "INF", StringComparison.Ordinal);
        public bool IsNaN => string.Equals(Sequence, "NAN", StringComparison.Ordinal);
        public bool IsTrue => string.Equals(Sequence, "true", StringComparison.Ordinal);
        public bool IsFalse => string.Equals(Sequence, "false", StringComparison.Ordinal);
        public bool IsSelf => string.Equals(Sequence, "self", StringComparison.Ordinal);
       
        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            if (SequenceBuilderLength == 0)
                return c == '_' || char.IsLetter(c);
            return c == '_' || char.IsLetterOrDigit(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
            state.PassLineFinish();
        }

        public override string ToString()
        {
            return $"{Sequence}";
        }
    }
}