namespace GDShrapt.Reader
{
    internal interface IPathReceiver
    {
        void HandleReceivedToken(GDPath token);
        void HandleReceivedIdentifierSkip();
    }
}
