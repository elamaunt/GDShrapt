namespace GDScriptConverter
{
    public class GDReturnStatement : GDStatement
    {
        protected internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"return";
        }
    }
}