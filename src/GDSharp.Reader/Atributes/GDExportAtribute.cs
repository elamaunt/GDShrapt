namespace GDSharp.Reader
{
    public class GDExportAtribute : GDClassMember
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
            // TODO:
            return $"export";
        }
    }
}