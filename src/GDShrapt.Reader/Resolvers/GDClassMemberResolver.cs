using System.Text;

namespace GDShrapt.Reader
{
    internal class GDClassMemberResolver : GDIntendedResolver
    {
        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        new IClassMembersReceiver Owner { get; }

        public GDClassMemberResolver(IClassMembersReceiver owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
            Owner = owner;
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            if (!IsSpace(c))
            {
                _sequenceBuilder.Append(c);
            }
            else
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                state.PassChar(c);
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                state.PassNewLine();
            }
            else
            {
                ResetIntendation();
                ResetSequence();
            }
        }

        private void ResetSequence()
        {
            _sequenceBuilder.Clear();
        }

        private void Complete(GDReadingState state, string sequence)
        {
            switch (sequence)
            {
                case "signal":
                    Owner.HandleReceivedToken(state.Push(new GDSignalDeclaration(LineIntendationThreshold + 1)));
                    break;
                case "enum":
                    Owner.HandleReceivedToken(state.Push(new GDEnumDeclaration(LineIntendationThreshold + 1)));
                    break;
                case "static":
                case "func":
                    Owner.HandleReceivedToken(state.Push(new GDMethodDeclaration(LineIntendationThreshold + 1)));
                    break;
                case "export":
                case "onready":
                case "const":
                case "var":
                    Owner.HandleReceivedToken(state.Push(new GDVariableDeclaration(LineIntendationThreshold + 1)));
                    break;
                case "class":
                    Owner.HandleReceivedToken(state.Push(new GDInnerClassDeclaration(LineIntendationThreshold + 1)));
                    break;
                default:
                    Owner.HandleReceivedToken(state.Push(new GDInvalidToken(x => x.IsNewLine())));
                    break;
            }
        }
    }
}