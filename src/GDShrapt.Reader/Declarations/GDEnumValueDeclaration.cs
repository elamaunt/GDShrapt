namespace GDShrapt.Reader
{
    public sealed class GDEnumValueDeclaration : GDNode
    {
        bool _keyChecked;
        public GDIdentifier Identifier { get; set; }
        public GDExpression Value { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == '}' || c == ',')
            {
                state.Pop();
                state.PassChar(c);
                return;
            }

            if (Identifier == null)
            {
                state.Push(Identifier = new GDIdentifier());
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
            if (Value == null)
                return $"{Identifier}";

            return $"{Identifier}: {Value}";
        }
    }
}