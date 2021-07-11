namespace GDShrapt.Reader
{
    public interface IIntendedTokenOrSkipReceiver<in T> : IIntendedTokenReceiver, ITokenOrSkipReceiver<T>
        where T : GDSyntaxToken
    {
    }

    public interface IIntendedTokenReceiver<in T> : IIntendedTokenReceiver, ITokenReceiver<T>
        where T : GDSyntaxToken
    {
    }

    public interface IIntendedTokenReceiver : INewLineReceiver, ITokenReceiver
    {
        void HandleReceivedToken(GDIntendation token);
    }
}
