using System.Text;

namespace GDShrapt.Reader
{
    internal class GDClassMembersResolver : GDIntendedResolver
    {
        bool _memberResolved;

        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        new IIntendedTokenReceiver<GDClassMember> Owner { get; }

        public GDClassMembersResolver(IIntendedTokenReceiver<GDClassMember> owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
            Owner = owner;
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            if (_memberResolved)
            {
                if (IsSpace(c))
                {
                    Owner.HandleReceivedToken(state.Push(new GDSpace()));
                    state.PassChar(c);
                    return;
                }

                Owner.ResolveInvalidToken(c, state, x => !x.IsSpace());
                return;
            }

            if (char.IsLetter(c))
            {
                _sequenceBuilder.Append(c);
            }
            else
            {
                if (_sequenceBuilder.Length > 0)
                {
                    var sequence = _sequenceBuilder.ToString();
                    ResetSequence();
                    Complete(state, sequence);
                    state.PassChar(c);
                }
                else
                {
                    if (IsSpace(c))
                    {
                        Owner.HandleReceivedToken(state.Push(new GDSpace()));
                    }
                    else
                    {
                        SendIntendationTokensToOwner();
                        Owner.HandleReceivedToken(state.Push(new GDInvalidToken(x => char.IsLetter(x) || x.IsSpace() || x.IsNewLine())));
                    }

                    state.PassChar(c);
                }
            }
        }

        internal override void HandleNewLineAfterIntendation(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                state.PassNewLine();
                return;
            }

            _memberResolved = false;
            ResetIntendation();
            state.PassNewLine();
        }

        internal override void HandleSharpCharAfterIntendation(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                state.PassSharpChar();
                return;
            }

            Owner.HandleReceivedToken(state.Push(new GDComment()));
            state.PassSharpChar();
        }

        private void ResetSequence()
        {
            _sequenceBuilder.Clear();
        }

        private void Complete(GDReadingState state, string sequence)
        {
            SendIntendationTokensToOwner();

            _memberResolved = true;
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
                        _memberResolved = false;

                        Owner.HandleReceivedToken(state.Push(new GDInvalidToken(x => x.IsSpace() || x.IsNewLine())));

                        if (sequence != null)
                            for (int i = 0; i < sequence.Length; i++)
                                state.PassChar(sequence[i]);
                    }
                    break;
            }
        }

        internal override void ForceComplete(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                ResetIntendation();
                return;
            }

            if (!_memberResolved)
                SendIntendationTokensToOwner();
            base.ForceComplete(state);
        }
    }
}