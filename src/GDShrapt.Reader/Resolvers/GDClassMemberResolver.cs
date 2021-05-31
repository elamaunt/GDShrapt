using System;
using System.Text;

namespace GDShrapt.Reader
{
    internal class GDClassMemberResolver : GDNode
    {
        readonly Action<GDClassMember> _handler;
        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        readonly int _lineIntendationThreshold;
        int _lineIntendation;
        bool _lineIntendationEnded;

        int _spaceCounter;

        bool _static;
        bool _onready;
        bool _export;

        public GDClassMemberResolver(int lineIntendation, Action<GDClassMember> handler)
        {
            _lineIntendationThreshold = lineIntendation;
            _handler = handler;
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            // Every member must start with line intendation equals intentation of parent plus 1
            if (!_lineIntendationEnded)
            {
                if (c == '\t')
                {
                    _spaceCounter = 0;
                    _lineIntendation++;
                    return;
                }
                else
                {
                    if (c == ' ' && state.Settings.ConvertFourSpacesIntoTabs)
                    {
                        _spaceCounter++;

                        if (_spaceCounter == 4)
                        {
                            _spaceCounter = 0;
                            HandleChar('\t', state);
                        }

                        return;
                    }
                    else
                    {
                        _lineIntendationEnded = true;

                        if (_lineIntendationThreshold != _lineIntendation)
                        {
                            state.PopNode();

                            // Pass all data to the previous node
                            state.PassLineFinish();

                            for (int i = 0; i < _lineIntendation; i++)
                                state.PassChar('\t');
                            for (int i = 0; i < _spaceCounter; i++)
                                state.PassChar(' ');

                            state.PassChar(c);
                            return;
                        }
                    }
                }
            }

            if (!IsSpace(c))
            {
                _sequenceBuilder.Append(c);
            }
            else
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (_sequenceBuilder?.Length > 0)
            {
                var sequence = _sequenceBuilder.ToString();
                ResetSequence();
                Complete(state, sequence);
                state.PassLineFinish();
            }
            else
            {
                _lineIntendation = 0;
                _lineIntendationEnded = false;
                _spaceCounter = 0;
                ResetSequence();
            }
        }

        private void ResetSequence()
        {
            _sequenceBuilder.Clear();
        }

        private void Complete(GDReadingState state, string sequence)
        {
            GDClassMember member = null;

            if (_export)
            {
                member = GetMemberForExport(sequence);
            }
            else
            {
                if (_onready)
                {
                    member = GetMemberForOnready(sequence);
                }
                else
                {
                    if (_static)
                    {
                        member = GetMemberForStatic(sequence);
                    }
                    else
                    {
                        member = GetFirstMember(sequence);
                    }
                }
            }

            if (member == null)
                return;

            _onready = false;
            _static = false;

            // Insert comment if exists
            member.EndLineComment = EndLineComment;
            EndLineComment = null;

            _handler(member);
            
            state.PushNode(member);
        }

        private GDClassMember GetFirstMember(string sequence)
        {
            switch (sequence)
            {
                // Modifiers
                case "onready":
                    _onready = true;
                    return null;
                case "static":
                    _static = true;
                    return null;
                case "export":
                    _export = true;
                    return null;

                case "class_name":
                    return new GDClassNameAtribute();
                case "extends":
                    return new GDExtendsAtribute();
                case "tool":
                    return new GDToolAtribute();
                case "func":
                    return new GDMethodDeclaration();
                case "const":
                    return new GDVariableDeclaration() { IsConstant = true };
                case "var":
                    return new GDVariableDeclaration();
                default:
                    return new GDInvalidMember(sequence);
            }
        }
        private GDClassMember GetMemberForExport(string sequence)
        {
            switch (sequence)
            {
                case "onready" when !_onready:
                    _onready = true;
                    return null;
                case "var":
                    return new GDVariableDeclaration()
                    {
                        HasOnReadyInitialization = _onready,
                        IsExported = _export
                    };
                default:
                    return new GDInvalidMember(sequence);
            }
        }

        private GDClassMember GetMemberForOnready(string sequence)
        {
            switch (sequence)
            {
                case "export" when !_export:
                    _export = true;
                    return null;
                case "var":
                    return new GDVariableDeclaration()
                    {
                        HasOnReadyInitialization = _onready,
                        IsExported = _export
                    };
                default:
                    return new GDInvalidMember(sequence);
            }
        }

        private GDClassMember GetMemberForStatic(string sequence)
        {
            switch (sequence)
            {
                case "func":
                    return new GDMethodDeclaration()
                    {
                        IsStatic = true
                    };
                default:
                    return new GDInvalidMember(sequence);
            }
        }
    }
}