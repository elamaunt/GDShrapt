namespace GDShrapt.Reader
{
    public class GDExtendsAtribute : GDClassMember
    {
        public GDType Type { get; set; }
        public GDString Path { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Path == null || Type == null)
            {
                if (c == '\"' || c == '\'')
                {
                    state.PushNode(Type = new GDType());
                    state.PassChar(c);
                    return;
                }
                else
                {
                    state.PushNode(Type = new GDType());
                    state.PassChar(c);
                    return;
                }
            }

            state.PopNode();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            if (Type != null)
                return $"extends {Type}";

            if (Path != null)
                return $"extends {Path}";

            return "extends";
        }
    }
}