namespace GDScriptConverter
{
    public abstract class GDPattern : GDCharSequence
    {
        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
        protected internal override void HandleChar(char c, GDReadingState state)
        {
            base.HandleChar(c, state);
        }
    }
}