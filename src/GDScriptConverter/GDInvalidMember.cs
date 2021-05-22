namespace GDScriptConverter
{
    public class GDInvalidMember : GDClassMember
    {
        public string Sequence { get; set; }

        public GDInvalidMember(string sequence)
        {
            Sequence = sequence;
        }

        public override void HandleChar(char c, GDReadingState state)
        {
            state.PopNode();
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
        }
    }
}