using System;

namespace GDShrapt.Reader
{
    internal class GDClassMemberResolver : GDCharSequence
    {
        private bool _staticFunc = false;
        private readonly Action<GDClassMember> _handler;

        public GDClassMemberResolver(Action<GDClassMember> handler)
        {
            _handler = handler;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            // Ignore space chars in start
            if (SequenceBuilder.Length == 0 && IsSpace(c))
                return;
            base.HandleChar(c, state);
        }

        internal override bool CanAppendChar(char c, GDReadingState state)
        {
            return c != ' ';
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            _staticFunc = false;
            CompleteSequence(state);
        }

        internal override void CompleteSequence(GDReadingState state)
        {
            base.CompleteSequence(state);

            var seq = Sequence;

            if (seq.IsNullOrWhiteSpace())
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
                    return;
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
                    member = new GDInvalidMember(seq);
                    break;
            }

            ResetSequence();

            member.EndLineComment = EndLineComment;
            EndLineComment = null;

            _handler(member);
            
            state.PushNode(member);
        }
    }
}