namespace GDShrapt.Reader
{
    public abstract class GDCommaSeparatedList<NODE> : GDSeparatedList<NODE, GDComma>,
        ITokenReceiver<GDNewLine>, 
        INewLineReceiver,
        ITokenReceiver<GDComma>
        where NODE : GDSyntaxToken
    {
        internal abstract GDReader ResolveNode();
        internal abstract bool IsStopChar(char c);

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                ListForm.Add(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            if (c == ',')
            {
                ListForm.Add(new GDComma());
                return;
            }
            else
            {
                if (!IsStopChar(c))
                {
                    state.PushAndPass(ResolveNode(), c);
                    return;
                }
            }

            state.PopAndPass(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            ListForm.Add(new GDNewLine());
        }

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDNewLine>.HandleReceivedToken(GDNewLine token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            ListForm.Add(token);
        }
    }
}
