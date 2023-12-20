using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDClassDeclaration : GDNode, IGDClassDeclaration,
        ITokenReceiver<GDClassAtributesList>,
        ITokenReceiver<GDClassMembersList>,
        ITokenReceiver<GDNewLine>,
        INewLineReceiver
    {
        public GDClassAtributesList Atributes
        {
            get => _form.Token0 ?? (_form.Token0 = new GDClassAtributesList(0));
            set => _form.Token0 = value;
        }

        public GDClassMembersList Members
        {
            get => _form.Token1 ?? (_form.Token1 = new GDClassMembersList(0));
            set => _form.Token1 = value;
        }

        public enum State
        {
            Atributes,
            Members,
            Completed
        }

        readonly GDTokensForm<State, GDClassAtributesList, GDClassMembersList> _form;
        public override GDTokensForm Form => _form; 
        public GDTokensForm<State, GDClassAtributesList, GDClassMembersList> TypedForm => _form;

        public GDClassDeclaration()
        {
            _form = new GDTokensForm<State, GDClassAtributesList, GDClassMembersList>(this);
        }

        public GDExtendsAttribute Extends => Atributes.OfType<GDExtendsAttribute>().FirstOrDefault();
        public GDClassNameAttribute ClassName => Atributes.OfType<GDClassNameAttribute>().FirstOrDefault();
        public bool IsTool => Atributes.OfType<GDToolAttribute>().Any();

        public IEnumerable<GDVariableDeclaration> Variables => Members.OfType<GDVariableDeclaration>();
        public IEnumerable<GDMethodDeclaration> Methods => Members.OfType<GDMethodDeclaration>();
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
                case State.Atributes:
                    _form.State = State.Members;
                    state.Push(Atributes);
                    state.PassChar(c);
                    break;
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

        void ITokenReceiver<GDClassAtributesList>.HandleReceivedToken(GDClassAtributesList token)
        {
            if (_form.IsOrLowerState(State.Atributes))
            {
                Atributes = token;
                _form.State = State.Members;
                return;
            }

            throw new GDInvalidStateException();
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