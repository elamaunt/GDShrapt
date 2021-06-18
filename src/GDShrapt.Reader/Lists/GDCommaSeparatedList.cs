namespace GDShrapt.Reader
{
    public abstract class GDCommaSeparatedList<NODE> : GDSeparatedList<NODE, GDComma>,
        ITokenReceiver<GDComma>
        where NODE : GDSyntaxToken
    {
        bool _checkedNextNode;
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

            if (!_checkedNextNode)
            {
                _checkedNextNode = true;
                state.Push(ResolveNode());
                state.PassChar(c);
                return;
            }
            else
            {
                if (c == ',')
                {
                    _checkedNextNode = false;
                    ListForm.Add(new GDComma());
                    return;
                }
                else
                {
                    if (!IsStopChar(c))
                    {
                        _checkedNextNode = false;
                        state.Push(ResolveNode());
                        state.PassChar(c);
                        return;
                    }
                }
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            ListForm.Add(new GDNewLine());
        }

        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            ListForm.Add(token);
        }

        void ITokenReceiver<GDComma>.HandleReceivedTokenSkip()
        {
            throw new GDInvalidReadingStateException();
        }
    }
}
