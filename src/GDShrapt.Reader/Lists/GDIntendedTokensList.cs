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
            ListForm.AddToEnd(token);
        }

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            ListForm.AddToEnd(token);
        }

        void ITokenReceiver<TOKEN>.HandleReceivedToken(TOKEN token)
        {
            ListForm.AddToEnd(token);
        }

        void ITokenReceiver.HandleReceivedToken(GDCarriageReturnToken token)
        {
            ListForm.AddToEnd(token);
        }
    }
}
