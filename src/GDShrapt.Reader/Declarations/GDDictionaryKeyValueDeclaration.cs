namespace GDShrapt.Reader
{
    public sealed class GDDictionaryKeyValueDeclaration : GDNode, IExpressionsReceiver
    {
        bool _keyChecked;

        public GDExpression Key { get; set; }
        public GDExpression Value { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == '}' || c ==',')
            {
                state.Pop();
                state.PassChar(c);
                return;
            }

            if (Key == null)
            {
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }

            if (!_keyChecked)
            {
                _keyChecked = c == ':' || c == '=';
                return;
            }

            if (Value == null)
            {
                state.Push(new GDExpressionResolver(this));
                state.PassChar(c);
                return;
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            // Ignore
        }

        public override string ToString()
        {
            return $"{Key}: {Value}";
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            throw new System.NotImplementedException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDComment token)
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            throw new System.NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDSpace token)
        {
            throw new System.NotImplementedException();
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}
