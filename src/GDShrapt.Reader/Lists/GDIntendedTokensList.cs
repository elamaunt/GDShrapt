namespace GDShrapt.Reader
{
    public abstract class GDIntendedTokensList<TOKEN> : GDSeparatedList<TOKEN, GDNewLine>,
        IIntendedTokenReceiver<TOKEN>
        where TOKEN : GDSyntaxToken
    {
        protected int LineIntendationThreshold { get; }

        internal GDIntendedTokensList(int lineIntendation)
        {
            LineIntendationThreshold = lineIntendation;
        }

        public GDIntendedTokensList()
        {

        }

        void IIntendedTokenReceiver.HandleReceivedToken(GDIntendation token)
        {
            ListForm.Add(token);
        }

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<TOKEN>.HandleReceivedToken(TOKEN token)
        {
            ListForm.Add(token);
        }
    }
}
