namespace GDShrapt.Reader
{
    public sealed class GDExpressionsList : GDSeparatedList<GDExpression, GDComma>, IExpressionsReceiver
    {
        bool _checkedNextExpression;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                ListForm.Add(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            if (!_checkedNextExpression)
            {
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }
            else
            {
                if (c == ',')
                {
                    _checkedNextExpression = false;
                    ListForm.Add(new GDComma());
                    return;
                }
                else
                {
                    if (!IsExpressionStopChar(c))
                    {
                        _checkedNextExpression = false;
                        state.Push(new GDExpressionResolver(this));
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

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (!_checkedNextExpression)
            {
                _checkedNextExpression = true;
                ListForm.Add(token);
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (!_checkedNextExpression)
            {
                _checkedNextExpression = true;
                return;
            }

            throw new GDInvalidReadingStateException();
        }
    }
}
