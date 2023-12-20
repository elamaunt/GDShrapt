namespace GDShrapt.Reader
{
    public interface ITokenReceiver<in T> : ITokenReceiver
        where T : GDSyntaxToken
    {
        void HandleReceivedToken(T token);
    }


    public interface ITokenReceiver
    {
        void HandleReceivedToken(GDComment token);
        void HandleReceivedToken(GDSpace token);
        void HandleReceivedToken(GDAttribute token);
        void HandleReceivedToken(GDInvalidToken token);
        void HandleReceivedToken(GDMultiLineSplitToken token);
    }
}
