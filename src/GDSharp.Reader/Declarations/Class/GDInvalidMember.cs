namespace GDSharp.Reader
{
    public class GDInvalidMember : GDClassMember
    {
        public string Sequence { get; set; }

        public GDInvalidMember(string sequence)
        {
            Sequence = sequence;
        }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            state.PopNode();
            state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.FinishLine();
        }
    }
}