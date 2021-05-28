namespace GDShrapt.Reader
{
    public class GDExportAtribute : GDClassMember
    {
        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleLineFinish(GDReadingState state)
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