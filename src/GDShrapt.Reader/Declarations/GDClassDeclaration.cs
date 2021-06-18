using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDClassDeclaration : GDNode
    {
        public GDClassAtributesList Atributes
        {
            get => _form.Token0 ?? (_form.Token0 = new GDClassAtributesList(0));
        }

        public GDClassMembersList Members
        {
            get => _form.Token1 ?? (_form.Token1 = new GDClassMembersList(0));
        }

        enum State
        {
            Atributes,
            Members,
            Completed
        }

        readonly GDTokensForm<State, GDClassAtributesList, GDClassMembersList> _form = new GDTokensForm<State, GDClassAtributesList, GDClassMembersList>();
        internal override GDTokensForm Form => _form;

        public GDExtendsAtribute Extends => Atributes.OfType<GDExtendsAtribute>().FirstOrDefault();
        public GDClassNameAtribute ClassName => Atributes.OfType<GDClassNameAtribute>().FirstOrDefault();
        public bool IsTool => Atributes.OfType<GDToolAtribute>().Any();

        public IEnumerable<GDVariableDeclaration> Variables => Members.OfType<GDVariableDeclaration>();
        public IEnumerable<GDMethodDeclaration> Methods => Members.OfType<GDMethodDeclaration>();
        public IEnumerable<GDEnumDeclaration> Enums => Members.OfType<GDEnumDeclaration>();
        public IEnumerable<GDInnerClassDeclaration> InnerClasses => Members.OfType<GDInnerClassDeclaration>();

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
                    throw new GDInvalidReadingStateException();
            }
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            _form.AddBeforeActiveToken(new GDNewLine());
        }
    }
}