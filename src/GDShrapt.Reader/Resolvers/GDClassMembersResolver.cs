using System;
using System.Text;

namespace GDShrapt.Reader
{
    internal class GDClassMembersResolver : GDIntendedResolver
    {
        bool _handleEndOfTheLineAsInvalid;

        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        new IIntendedTokenReceiver<GDClassMember> Owner { get; }

        bool _staticMet;
        GDSpace _spaceAfterStatic;

        public GDClassMembersResolver(IIntendedTokenReceiver<GDClassMember> owner, int lineIntendation)
            : base(owner, lineIntendation)
        {
            Owner = owner;
        }

        internal override void HandleCharAfterIntendation(char c, GDReadingState state)
        {
            if (_handleEndOfTheLineAsInvalid)
            {
                if (IsSpace(c))
                {
                    Owner.HandleReceivedToken(state.Push(new GDSpace()));
                    state.PassChar(c);
                    return;
                }

                Owner.HandleAsInvalidToken(c, state, x => x.IsSpace() || x.IsNewLine());
                return;
            }

            if (c == '@' || char.IsLetter(c) || c == '_')
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
                        if (_staticMet)
                        {
                            state.Push(_spaceAfterStatic = new GDSpace());
                        }
                        else
                        {
                            Owner.HandleReceivedToken(state.Push(new GDSpace()));
                        }
                    }
                    else
                    {
                        SendIntendationTokensToOwner();
                        Owner.HandleReceivedToken(state.Push(new GDInvalidToken(x => x == '@' || char.IsLetter(x) || x.IsSpace() || x.IsNewLine())));
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

            _handleEndOfTheLineAsInvalid = false;
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

        internal override void HandleLeftSlashCharAfterIntendation(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                state.PassLeftSlashChar();
                return;
            }

            Owner.HandleReceivedToken(state.Push(new GDMultiLineSplitToken()));
            state.PassLeftSlashChar();
        }

        private void ResetSequence()
        {
            _sequenceBuilder.Clear();
        }

        private void Complete(GDReadingState state, string sequence)
        {
            SendIntendationTokensToOwner();

            if (sequence[0] == '@')
            {
                Owner.HandleReceivedToken(state.Push(new GDCustomAttribute(CurrentResolvedIntendationInSpaces)));

                for (int i = 0; i < sequence.Length; i++)
                    state.PassChar(sequence[i]);

                return;
            }

            _handleEndOfTheLineAsInvalid = true;

            void HandleStaticIfMet(ITokenReceiver<GDSpace> spaceReceiver, Action push, bool invalid = true)
            {
                if (!_staticMet)
                {
                    push();
                    return;
                }

                if (invalid)
                {
                    Owner.HandleReceivedToken(state.Push(new GDInvalidToken(x => x.IsNewLine() || x.IsSpace())));

                    var s = "static";
                    for (int i = 0; i < s.Length; i++)
                        state.PassChar(s[i]);

                    if (_spaceAfterStatic != null)
                    {
                        var spaceSeq = _spaceAfterStatic.Sequence;

                        for (int i = 0; i < spaceSeq.Length; i++)
                            state.PassChar(spaceSeq[i]);

                        _spaceAfterStatic = null;
                    }

                    push();
                }
                else
                {
                    push();

                    var s = "static";
                    for (int i = 0; i < s.Length; i++)
                        state.PassChar(s[i]);

                    if (_spaceAfterStatic != null)
                    {
                        if (spaceReceiver != null)
                            spaceReceiver.Add(_spaceAfterStatic);
                        else
                            throw new GDInvalidStateException();

                        _spaceAfterStatic = null;
                    }
                }

                _staticMet = false;
            }

            switch (sequence)
            {
                case "@icon":
                    {
                        var a = new GDCustomAttribute();
                        HandleStaticIfMet(a, () => Owner.HandleReceivedToken(state.Push(a)));
                        a.Add(new GDAttribute().AddAt().Add(new GDIdentifier() { Sequence = "icon" }));

                        _handleEndOfTheLineAsInvalid = false;
                    }
                    break;
                case "class_name":
                    {
                        var a = new GDClassNameAttribute();
                        HandleStaticIfMet(a, () => Owner.HandleReceivedToken(state.Push(a)));
                        a.Add(new GDClassNameKeyword());

                        _handleEndOfTheLineAsInvalid = false;
                    }
                    break;
                case "extends":
                    {
                        var a = new GDExtendsAttribute();
                        HandleStaticIfMet(a, () => Owner.HandleReceivedToken(state.Push(a)));
                        a.Add(new GDExtendsKeyword());

                        _handleEndOfTheLineAsInvalid = false;
                    }
                    break;
                case "tool":
                    {
                        var a = new GDToolAttribute();
                        HandleStaticIfMet(a, () => Owner.HandleReceivedToken(state.Push(a)));
                        a.Add(new GDToolKeyword());

                        _handleEndOfTheLineAsInvalid = false;
                    }
                    break;
                case "signal":
                    {
                        var m = new GDSignalDeclaration(CurrentResolvedIntendationInSpaces);
                        HandleStaticIfMet(m, () => Owner.HandleReceivedToken(state.Push(m)));
                        m.Add(new GDSignalKeyword());
                    }
                    break;
                case "enum":
                    {
                        var m = new GDEnumDeclaration(CurrentResolvedIntendationInSpaces);
                        HandleStaticIfMet(m, () => Owner.HandleReceivedToken(state.Push(m)));
                        m.Add(new GDEnumKeyword());
                    }
                    break;
                case "static":
                    {
                        _handleEndOfTheLineAsInvalid = false;
                        _sequenceBuilder.Clear();

                        if (_staticMet)
                        {
                            Owner.HandleReceivedToken(new GDInvalidToken("static"));

                            if (_spaceAfterStatic != null)
                            {
                                Owner.HandleReceivedToken(_spaceAfterStatic);
                                _spaceAfterStatic = null;
                            }
                        }

                        _staticMet = true;
                    }
                    break;
                case "func":
                    {
                        var m = new GDMethodDeclaration(CurrentResolvedIntendationInSpaces);
                        HandleStaticIfMet(m, () => Owner.HandleReceivedToken(state.Push(m)), false);
                        m.Add(new GDFuncKeyword());
                    }
                    break;
                case "const":
                    {
                        var m = new GDVariableDeclaration(CurrentResolvedIntendationInSpaces);
                        HandleStaticIfMet(m, () => Owner.HandleReceivedToken(state.Push(m)));
                        m.Add(new GDConstKeyword());
                    }
                    break;
                case "var":
                    {
                        var m = new GDVariableDeclaration(CurrentResolvedIntendationInSpaces);
                        HandleStaticIfMet(m, () => Owner.HandleReceivedToken(state.Push(m)), false);
                        m.Add(new GDVarKeyword());
                    }
                    break;
                case "class":
                    {
                        var m = new GDInnerClassDeclaration(CurrentResolvedIntendationInSpaces);
                        HandleStaticIfMet(m, () => Owner.HandleReceivedToken(state.Push(m)));
                        m.Add(new GDClassKeyword());
                    }
                    break;
                default:
                    {
                        _handleEndOfTheLineAsInvalid = false;

                        HandleStaticIfMet(null, () => Owner.HandleReceivedToken(state.Push(new GDInvalidToken(x => x.IsSpace() || x.IsNewLine()))));

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

            if (!_handleEndOfTheLineAsInvalid)
                SendIntendationTokensToOwner();
            base.ForceComplete(state);
        }
    }
}