namespace GDShrapt.Reader
{
    public class GDClassAtributesList : GDSeparatedList<GDClassAtribute, GDNewLine>
    {
        private int _lineIntendationThreshold;
        bool _completed;

        internal GDClassAtributesList(int lineIntendation)
        {
            _lineIntendationThreshold = lineIntendation;
        }

        public GDClassAtributesList()
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
