namespace GDShrapt.Reader
{
    public interface ILeftSlashReceiver : ITokenReceiver
    {
        void HandleReceivedToken(GDLeftSlash token);
    }
}