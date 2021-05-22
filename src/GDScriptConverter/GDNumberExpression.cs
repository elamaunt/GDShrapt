namespace GDScriptConverter
{
    public class GDNumberExpression : GDCharSequenceNode
    {
        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return char.IsDigit(c);
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}