namespace GDScriptConverter
{
    public class GDClassMemberResolver : GDCharSequenceNode
    {
        private bool _staticFunc = false;

        public GDClassDeclaration GDClass { get; }

        public GDClassMemberResolver(GDClassDeclaration gDClass)
        {
            GDClass = gDClass;
        }

        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return c != ' ';
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
        }

        protected override void CompleteSequence(GDReadingState state)
        {
            base.CompleteSequence(state);

            GDClassMember member = null;

            switch (Sequence)
            {
                 case "extends":
                    member = new GDExtendsAtribute();
                    break;
                case "tool":
                    member = new GDToolAtribute();
                    break;
                case "static":
                    _staticFunc = true;
                    break;
                case "func":
                    member = new GDMethodDeclaration()
                    {
                        IsStatic = _staticFunc
                    };
                    break;
                case "const":
                    member = new GDVariableDeclaration()
                    { 
                        IsConstant = true
                    };
                    break;
                case "var":
                    member = new GDVariableDeclaration();
                    break;
                default:
                    member = new GDInvalidMember(Sequence);
                    break;
            }

            GDClass.Members.Add(member);
            state.PushNode(member);
        }
    }
}