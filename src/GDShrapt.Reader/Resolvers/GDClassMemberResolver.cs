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
                    {
                        var m = new GDSignalDeclaration(LineIntendationThreshold);
                        m.SendKeyword(new GDSignalKeyword());
                        Owner.HandleReceivedToken(state.Push(m));
                        break;
                    }
                case "enum":
                    {
                        var m = new GDEnumDeclaration(LineIntendationThreshold);
                        m.SendKeyword(new GDEnumKeyword());
                        Owner.HandleReceivedToken(state.Push(m));
                        break;
                    }
                case "static":
                    {
                        var m = new GDMethodDeclaration(LineIntendationThreshold);
                        m.SendKeyword(new GDStaticKeyword());
                        Owner.HandleReceivedToken(state.Push(m));
                        break;
                    }
                case "func":
                    {
                        var m = new GDMethodDeclaration(LineIntendationThreshold);
                        m.SendKeyword(new GDFuncKeyword());
                        Owner.HandleReceivedToken(state.Push(m));
                        break;
                    }
                case "export":
                    {
                        Owner.HandleReceivedToken(state.Push(new GDVariableDeclaration(LineIntendationThreshold)));

                        for (int i = 0; i < sequence.Length; i++)
                            state.PassChar(sequence[i]);
                    }
                    break;
                case "onready":
                    {
                        var m = new GDVariableDeclaration(LineIntendationThreshold);
                        m.SendKeyword(new GDOnreadyKeyword());
                        Owner.HandleReceivedToken(state.Push(m));

                        break;
                    }
                case "const":
                    {
                        var m = new GDVariableDeclaration(LineIntendationThreshold);
                        m.SendKeyword(new GDConstKeyword());
                        Owner.HandleReceivedToken(state.Push(m));
                    }
                    break;
                case "var":
                    {
                        var m = new GDVariableDeclaration(LineIntendationThreshold);
                        m.SendKeyword(new GDVarKeyword());
                        Owner.HandleReceivedToken(state.Push(m));
                    }
                    break;
                case "class":
                    {
                        var m = new GDInnerClassDeclaration(LineIntendationThreshold);
                        m.SendKeyword(new GDClassKeyword());
                        Owner.HandleReceivedToken(state.Push(m));
                    }
                    break;
                default:
                    {
                        Owner.HandleReceivedToken(state.Push(new GDInvalidToken(x => x.IsNewLine())));
                       
                        if (sequence != null)
                            for (int i = 0; i < sequence.Length; i++)
                                state.PassChar(sequence[i]);
                    }
                    break;
            }
        }
    }
}