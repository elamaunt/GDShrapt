namespace GDShrapt.Reader
{
    public interface IIntendedTokenOrSkipReceiver<T> : IIntendedTokenReceiver, ITokenOrSkipReceiver<T>
        where T : GDSyntaxToken
    {
    }

    public interface IIntendedTokenReceiver<T> : IIntendedTokenReceiver, ITokenReceiver<T>
        where T : GDSyntaxToken
    {
    }

    public interface IIntendedTokenReceiver : INewLineReceiver, ITokenReceiver
    {
        void HandleReceivedToken(GDIntendation token);
    }
}
