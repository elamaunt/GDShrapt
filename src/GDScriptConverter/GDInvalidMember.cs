namespace GDScriptConverter
{
    public class GDInvalidMember : GDClassMember
    {
        public GDClassDeclaration GDClass { get; }
        public string Sequence { get; set; }

        public GDInvalidMember(GDClassDeclaration gDClass, string sequence)
        {
            GDClass = gDClass;
            Sequence = sequence;
        }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            state.PopNode();
            state.PushNode(new GDClassMemberResolver(GDClass));
            state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            // Ignore
        }
    }
}