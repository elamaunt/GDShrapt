using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDClassDeclaration : GDNode, IGDClassDeclaration,
        ITokenReceiver<GDClassMembersList>,
        ITokenReceiver<GDNewLine>,
        INewLineReceiver
    {
        public GDClassMembersList Members
        {
            get => _form.GetOrInit(0, new GDClassMembersList(0));
            set => _form.Token0 = value;
        }

        public enum State
        {
            Members,
            Completed
        }

        readonly GDTokensForm<State, GDClassMembersList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDClassMembersList> TypedForm => _form;

        public GDClassDeclaration()
        {
            _form = new GDTokensForm<State, GDClassMembersList>(this);
        }

        public GDExtendsAttribute Extends => Attributes.OfType<GDExtendsAttribute>().FirstOrDefault();
        public GDClassNameAttribute ClassName => Attributes.OfType<GDClassNameAttribute>().FirstOrDefault();

        public bool IsTool => Attributes.OfType<GDToolAttribute>().Any();

        public IEnumerable<GDClassAttribute> Attributes => Members.OfType<GDClassAttribute>();
        public IEnumerable<GDCustomAttribute> CustomAttributes => Members.OfType<GDCustomAttribute>();

        public IEnumerable<GDVariableDeclaration> Variables => Members.OfType<GDVariableDeclaration>();
        public IEnumerable<GDMethodDeclaration> Methods => Members.OfType<GDMethodDeclaration>();
        public IEnumerable<GDSignalDeclaration> Signals => Members.OfType<GDSignalDeclaration>();
        public IEnumerable<GDEnumDeclaration> Enums => Members.OfType<GDEnumDeclaration>();
        public IEnumerable<GDInnerClassDeclaration> InnerClasses => Members.OfType<GDInnerClassDeclaration>();
        public IEnumerable<GDIdentifiableClassMember> IdentifiableMembers => Members.OfType<GDIdentifiableClassMember>();
        public override GDNode CreateEmptyInstance()
        {
            return new GDClassDeclaration();
        }

        internal override void Visit(IGDVisitor visitor)
        {
            visitor.Visit(this);
        }

        internal override void Left(IGDVisitor visitor)
        {
            visitor.Left(this);
        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.Members:
                    _form.State = State.Completed;
                    state.Push(Members);
                    state.PassChar(c);
                    break;
                default:
                    throw new GDInvalidStateException();
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _form.AddBeforeActiveToken(new GDNewLine());
        }

        internal override void HandleCarriageReturnChar(GDReadingState state)
        {
            _form.AddBeforeActiveToken(new GDCarriageReturnToken());
        }

        void ITokenReceiver<GDClassMembersList>.HandleReceivedToken(GDClassMembersList token)
        {
            if (_form.StateIndex <= (int)State.Members)
            {
                Members = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidStateException();
        }

        void ITokenReceiver<GDNewLine>.HandleReceivedToken(GDNewLine token)
        {
            _form.AddBeforeActiveToken(new GDNewLine());
        }

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            _form.AddBeforeActiveToken(new GDNewLine());
        }

        GDTypeNode IGDClassDeclaration.BaseType => Extends?.Type;
        GDIdentifier IGDClassDeclaration.Identifier => ClassName?.Identifier;
    }
}