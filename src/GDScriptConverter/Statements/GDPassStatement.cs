namespace GDScriptConverter
{
    public class GDPassStatement : GDStatement
    {
        protected internal override void HandleChar(char c, GDReadingState state)
        {
            state.PopNode();
            state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}