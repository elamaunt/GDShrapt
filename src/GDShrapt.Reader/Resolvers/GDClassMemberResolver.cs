using System.Text;

namespace GDShrapt.Reader
{
    internal class GDClassMemberResolver : GDIntendedResolver
    {
        readonly StringBuilder _sequenceBuilder = new StringBuilder();

        bool _static;
        bool _onready;
        bool _export;

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
            (GDClassMember member, bool valid) x;

            if (_export)
            {
                x = GetMemberForExport(sequence);
            }
            else
            {
                if (_onready)
                {
                    x = GetMemberForOnready(sequence);
                }
                else
                {
                    if (_static)
                    {
                        x = GetMemberForStatic(sequence);
                    }
                    else
                    {
                        x = GetFirstMember(sequence);
                    }
                }
            }

            if (x.valid)
            {

                if (x.member == null)
                    return;

                _onready = false;
                _static = false;

                Owner.HandleReceivedToken(x.member);
                state.Push(x.member);
            }
            else
            {
                AppendAndPush(new GDInvalidToken(' '), state);
            }
        }

        private (GDClassMember, bool) GetFirstMember(string sequence)
        {
            switch (sequence)
            {
                // Modifiers
                case "onready":
                    _onready = true;
                    return (null, true);
                case "static":
                    _static = true;
                    return (null, true);
                case "export":
                    _export = true;
                    return (null, true);

                case "class_name":
                    return (new GDClassNameAtribute(), true);
                case "extends":
                    return (new GDExtendsAtribute(), true);
                case "tool":
                    return (new GDToolAtribute(), true);
                case "signal":
                    return (new GDSignalDeclaration(), true);
                case "enum":
                    return (new GDEnumDeclaration(), true);
                case "func":
                    return (new GDMethodDeclaration(), true);
                case "const":
                    return (new GDVariableDeclaration() { IsConstant = true }, true);
                case "var":
                    return (new GDVariableDeclaration(), true);
            default:
                    return (null, false);
            }
        }
        private (GDClassMember, bool) GetMemberForExport(string sequence)
        {
            switch (sequence)
            {
                case "onready" when !_onready:
                    _onready = true;
                    return (null, true);
                case "var":
                    return (new GDVariableDeclaration()
                    {
                        HasOnReadyInitialization = _onready,
                        IsExported = _export
                    }, true);
                default:
                    return (null, false);
            }
        }

        private (GDClassMember, bool) GetMemberForOnready(string sequence)
        {
            switch (sequence)
            {
                case "export" when !_export:
                    _export = true;
                    return (null, true);
                case "var":
                    return (new GDVariableDeclaration()
                    {
                        HasOnReadyInitialization = _onready,
                        IsExported = _export
                    }, true);
                default:
                    return (null, false);
            }
        }

        private (GDClassMember, bool) GetMemberForStatic(string sequence)
        {
            switch (sequence)
            {
                case "func":
                    return (new GDMethodDeclaration()
                    {
                        IsStatic = true
                    }, true);
                default:
                    return (null, false);
            }
        }
    }
}