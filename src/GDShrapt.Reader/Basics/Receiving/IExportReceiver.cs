namespace GDShrapt.Reader
{
    internal interface IExportReceiver : IStyleTokensReceiver
    {
        void HandleReceivedExport(GDExportDeclaration token);
        void HandleReceivedExportSkip();
    }
}
