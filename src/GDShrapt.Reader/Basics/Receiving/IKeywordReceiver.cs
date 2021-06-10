namespace GDShrapt.Reader
{
    internal interface IKeywordReceiver<KEYWORD> : IStyleTokensReceiver 
        where KEYWORD : IGDKeywordToken
    {
        void HandleReceivedToken(KEYWORD token);
        void HandleReceivedKeywordSkip();
    }
}
