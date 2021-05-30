namespace GDShrapt.Reader
{
    public class GDString : GDCharSequence
    {
        public bool Multiline { get; set; }

        internal override void HandleLineFinish(GDReadingState state)
        {
            Append('\n');
        }

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return c != '"';
        }

        internal override void HandleSharpChar(GDReadingState state)
        {
            Append('#');
        }

        public override string ToString()
        {
            return $"\"{Sequence}\"";
        }
    }
}