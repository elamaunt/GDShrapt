using System;

namespace GDShrapt.Reader
{
    public sealed class GDSpace : GDCharSequence
    {
        public new string Sequence
        {
            get => base.Sequence;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    throw new FormatException("Invalid space format.");
                base.Sequence = value;
            }
        }

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return IsSpace(c);
        }

        public static GDSpace operator +(GDSpace one, GDSpace other)
        {
            one.Sequence += other.Sequence;
            return one;
        }

        public override GDSyntaxToken Clone()
        {
            return new GDSpace()
            { 
                Sequence = Sequence
            };
        }
    }
}
