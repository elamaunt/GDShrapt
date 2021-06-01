namespace GDShrapt.Reader
{
    public class GDDictionaryKeyValueDeclaration : GDNode
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
                state.PopNode();
                state.PassChar(c);
                return;
            }

            if (Key == null)
            {
                state.PushNode(new GDExpressionResolver(expr => Key = expr));
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
                state.PushNode(new GDExpressionResolver(expr => Value = expr));
                state.PassChar(c);
                return;
            }

            state.PopNode();
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
