namespace GDShrapt.Reader
{
    public sealed class GDParametersList : GDSeparatedList<GDParameterDeclaration, GDComma>,
        ITokenReceiver<GDParameterDeclaration>,
        ITokenReceiver<GDComma>
    {
        bool _completed;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDExpressionResolver(this));
                state.PassNewLine();
                return;
            }

            state.Pop();
            state.PassNewLine();
        }

        void ITokenReceiver<GDParameterDeclaration>.HandleReceivedToken(GDParameterDeclaration token)
        {
            throw new System.NotImplementedException();
        }

        void ITokenReceiver<GDParameterDeclaration>.HandleReceivedTokenSkip()
        {
            throw new System.NotImplementedException();
        }
        void ITokenReceiver<GDComma>.HandleReceivedToken(GDComma token)
        {
            throw new System.NotImplementedException();
        }

        void ITokenReceiver<GDComma>.HandleReceivedTokenSkip()
        {
            throw new System.NotImplementedException();
        }
    }
}
