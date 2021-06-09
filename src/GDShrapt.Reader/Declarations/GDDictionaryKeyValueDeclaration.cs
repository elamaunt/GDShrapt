namespace GDShrapt.Reader
{
    public sealed class GDDictionaryKeyValueDeclaration : GDNode
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

        internal override void HandleLineFinish(GDReadingState state)
        {
            // Ignore
        }

        public override string ToString()
        {
            return $"{Key}: {Value}";
        }
    }
}
