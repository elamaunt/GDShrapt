using System;

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

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            // Ignore space chars in start
            if (SequenceBuilder.Length == 0 && IsSpace(c))
                return;
            base.HandleChar(c, state);
        }

        protected override bool CanAppendChar(char c, GDReadingState state)
        {
            return c != ' ';
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteSequence(state);
        }

        protected override void CompleteSequence(GDReadingState state)
        {
            base.CompleteSequence(state);

            if (Sequence.IsNullOrWhiteSpace())
                return;

            GDClassMember member = null;

            switch (Sequence)
            {
                case "class_name":
                    member = new GDClassNameAtribute();
                    break;
                case "extends":
                    member = new GDExtendsAtribute();
                    break;
                case "tool":
                    member = new GDToolAtribute();
                    break;
                case "static":
                    _staticFunc = true;
                    ResetSequence();
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
                case "export":
                    member = new GDExportAtribute();
                    break;
                default:
                    member = new GDInvalidMember(GDClass, Sequence);
                    break;
            }

            
            GDClass.Members.Add(member);
            state.PushNode(member);
        }
    }
}