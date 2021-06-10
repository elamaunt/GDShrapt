namespace GDShrapt.Reader
{
    public class GDStatementsList : GDSeparatedList<GDStatement, GDNewLine>, IStatementsReceiver
    {
        private int _lineIntendationThreshold;

        internal GDStatementsList(int lineIntendation)
        {
            _lineIntendationThreshold = lineIntendation;
        }

        public GDStatementsList()
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

        void IStatementsReceiver.HandleReceivedToken(GDStatement token)
        {
            TokensList.AddLast(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            TokensList.AddLast(token);
        }
    }
}
