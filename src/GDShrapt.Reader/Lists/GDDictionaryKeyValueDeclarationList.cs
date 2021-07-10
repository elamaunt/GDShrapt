namespace GDShrapt.Reader
{
    public sealed class GDDictionaryKeyValueDeclarationList : GDSeparatedList<GDDictionaryKeyValueDeclaration, GDComma>
    {
        public GDDictionaryKeyValueDeclarationList()
        { 
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (c == ',')
            {
                ListForm.Add(new GDComma());
                return;
            }

            if (c == '}')
            {
                state.PopAndPass(c);
                return;
            }

            ListForm.Add(state.PushAndPass(new GDDictionaryKeyValueDeclaration(), c));
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            ListForm.Add(new GDNewLine());
        }
        public override GDNode CreateEmptyInstance()
        {
            return new GDDictionaryKeyValueDeclarationList();
        }

        /*void ITokenReceiver<GDDictionaryKeyValueDeclaration>.HandleReceivedToken(GDDictionaryKeyValueDeclaration token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDDictionaryKeyValueDeclaration>.HandleReceivedTokenSkip()
        {
        }

        void ITokenReceiver<GDComma>.HandleReceivedTokenSkip()
        {
        }*/
    }
}
