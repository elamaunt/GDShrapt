namespace GDShrapt.Reader
{
    public interface ITokenOrSkipReceiver<T> : ITokenReceiver<T>, ITokenSkipReceiver<T>
        where T : GDSyntaxToken
    {
    }
}