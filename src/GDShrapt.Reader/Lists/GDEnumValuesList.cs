namespace GDShrapt.Reader
{
    public sealed class GDEnumValuesList : GDSeparatedList<GDEnumValueDeclaration, GDNewLine>,
        ITokenReceiver<GDEnumValueDeclaration>,
        ITokenReceiver<GDNewLine>
    {
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

            ListForm.Add(state.PushAndPass(new GDEnumValueDeclaration(), c));
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            ListForm.Add(new GDNewLine());
        }

        void ITokenReceiver<GDEnumValueDeclaration>.HandleReceivedToken(GDEnumValueDeclaration token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDNewLine>.HandleReceivedToken(GDNewLine token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDEnumValueDeclaration>.HandleReceivedTokenSkip()
        {
        }

        void ITokenReceiver<GDNewLine>.HandleReceivedTokenSkip()
        {
        }
    }
}
