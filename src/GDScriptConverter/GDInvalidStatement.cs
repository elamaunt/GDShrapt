namespace GDScriptConverter
{
    public class GDInvalidStatement : GDStatement
    {
        public string Sequence { get; }

        public GDInvalidStatement(string sequence)
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