using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDClassDeclaration : GDNode,
        ITokenReceiver<GDClassAtributesList>,
        ITokenReceiver<GDClassMembersList>,
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

        enum State
        {
            Atributes,
            Members,
            Completed
        }

        readonly GDTokensForm<State, GDClassAtributesList, GDClassMembersList> _form;
        public override GDTokensForm Form => _form;

        public GDClassDeclaration()
        {
            _form = new GDTokensForm<State, GDClassAtributesList, GDClassMembersList>(this);
        }

        public GDExtendsAtribute Extends => Atributes.OfType<GDExtendsAtribute>().FirstOrDefault();
        public GDClassNameAtribute ClassName => Atributes.OfType<GDClassNameAtribute>().FirstOrDefault();
        public bool IsTool => Atributes.OfType<GDToolAtribute>().Any();

        public IEnumerable<GDVariableDeclaration> Variables => Members.OfType<GDVariableDeclaration>();
        public IEnumerable<GDMethodDeclaration> Methods => Members.OfType<GDMethodDeclaration>();
        public IEnumerable<GDEnumDeclaration> Enums => Members.OfType<GDEnumDeclaration>();
        public IEnumerable<GDInnerClassDeclaration> InnerClasses => Members.OfType<GDInnerClassDeclaration>();

        public override GDNode CreateEmptyInstance()
        {
            return new GDClassDeclaration();
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
            if (_form.State == State.Atributes)
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

        void INewLineReceiver.HandleReceivedToken(GDNewLine token)
        {
            _form.AddBeforeActiveToken(new GDNewLine());
        }
    }
}