namespace GDShrapt.Reader
{
    public interface ITokenOrSkipReceiver<in T> : ITokenReceiver<T>, ITokenSkipReceiver<T>
        where T : GDSyntaxToken
    {
    }
}