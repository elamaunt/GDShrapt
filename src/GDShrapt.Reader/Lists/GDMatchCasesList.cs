namespace GDShrapt.Reader
{
    public class GDMatchCasesList : GDSeparatedList<GDStatement, GDNewLine>
    {
        private int _lineIntendationThreshold;
        bool _completed;

        internal GDMatchCasesList(int lineIntendation)
        {
            _lineIntendationThreshold = lineIntendation;
        }

        public GDMatchCasesList()
        {
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
