namespace GDShrapt.Reader
{
    internal interface IStructureTokensReceiver<TOKEN> : IStyleTokensReceiver
        where TOKEN : IGDStructureToken
    {
        void HandleReceivedToken(GDCloseBracket token);
        void HandleReceivedToken(GDOpenBracket token);
        void HandleReceivedToken(GDColon token);
        void HandleReceivedToken(GDSemiColon token);
    }
}