namespace GDShrapt.Reader
{
    public class GDExtendsAtribute : GDClassMember
    {
        public GDType Type { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Type == null)
            {
                state.PushNode(Type = new GDType());
                state.PassChar(c);
                return;
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
            return $"extends {Type}";
        }
    }
}