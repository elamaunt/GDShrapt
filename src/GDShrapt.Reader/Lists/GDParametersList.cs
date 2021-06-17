namespace GDShrapt.Reader
{
    public sealed class GDParametersList : GDSeparatedList<GDParameterDeclaration, GDComma>,
        ITokenReceiver<GDParameterDeclaration>,
        ITokenReceiver<GDComma>
    {
        internal override void HandleChar(char c, GDReadingState state)
        {
            if (c == ',')
            {
                return;
            }

            if (c == ')')
            {
                state.PopAndPass(c);
                return;
            }

            if (c.IsIdentifierStartChar())
                ListForm.Add(state.PushAndPass(new GDParameterDeclaration(), c));
            else
                this.ResolveInvalidToken(c, state, x => x == ',' || x == ')' || x.IsIdentifierStartChar());
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            ListForm.Add(new GDNewLine());
        }

        void ITokenReceiver<GDParameterDeclaration>.HandleReceivedToken(GDParameterDeclaration token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDParameterDeclaration>.HandleReceivedTokenSkip()
        {

        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDComma>.HandleReceivedTokenSkip()
        {

        }
    }
}
