namespace GDShrapt.Reader
{
    public interface INewLineReceiver : ITokenReceiver
    {
        void HandleReceivedToken(GDNewLine token);
    }
}