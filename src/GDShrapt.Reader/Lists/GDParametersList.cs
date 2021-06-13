namespace GDShrapt.Reader
{
    public sealed class GDParametersList : GDSeparatedList<GDParameterDeclaration, GDComma>
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

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (!_completed)
            {
                _completed = true;
                state.Push(new GDExpressionResolver(this));
                state.PassLineFinish();
                return;
            }

            state.Pop();
            state.PassLineFinish();
        }
    }
}
