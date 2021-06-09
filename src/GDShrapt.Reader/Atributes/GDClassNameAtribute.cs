namespace GDShrapt.Reader
{
    public class GDClassNameAtribute : GDClassMember
    {
        public GDIdentifier Identifier { get; set; }
        public GDString Path { get; set; }

        public GDString Icon { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Identifier == null && Path == null)
            {
                if (c == '\"' || c == '\'')
                {
                    state.Push(Path = new GDString());
                    state.PassChar(c);
                    return;
                }
                else
                {
                    state.Push(Identifier = new GDIdentifier());
                    state.PassChar(c);
                    return;
                }
            }

            if (c == ',' && Icon == null)
            {
                state.Push(Icon = new GDString());
                return;
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            if (Icon != null)
                return $"class_name {(GDSimpleSyntaxToken)Identifier ?? Path}, {Icon}";

            return $"class_name {(GDSimpleSyntaxToken)Identifier ?? Path}";
        }
    }
}