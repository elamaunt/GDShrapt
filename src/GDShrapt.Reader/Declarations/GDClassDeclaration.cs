using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDClassDeclaration : GDNode
    {
        bool _membersChecked;

        readonly GDTokensForm<GDClassMembersList> _form = new GDTokensForm<GDClassMembersList>();

        State _state;

        enum State
        {
            Members,
            Copmleted
        }

        public GDClassMembersList Members
        {
            get => _form.Token0 ?? (_form.Token0 = new GDClassMembersList());
        }

        public GDExtendsAtribute Extends => Members.OfType<GDExtendsAtribute>().FirstOrDefault();
        public GDClassNameAtribute ClassName => Members.OfType<GDClassNameAtribute>().FirstOrDefault();
        public bool IsTool => Members.OfType<GDToolAtribute>().Any();

        public IEnumerable<GDVariableDeclaration> Variables => Members.OfType<GDVariableDeclaration>();
        public IEnumerable<GDMethodDeclaration> Methods => Members.OfType<GDMethodDeclaration>();
        public IEnumerable<GDEnumDeclaration> Enums => Members.OfType<GDEnumDeclaration>();
        public IEnumerable<GDInnerClassDeclaration> InnerClasses => Members.OfType<GDInnerClassDeclaration>();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeToken(state.Push(new GDSpace()), (int)_state);
                state.PassChar(c);
                return;
            }

            switch (_state)
            {
                case State.Members:
                    break;
                case State.Copmleted:
                    break;
                default:
                    break;
            }

            // Old code
            /*
            if (!_membersChecked)
            {
                _membersChecked = true;
                state.Push(new GDClassMemberResolver(this, 0));
                state.PassChar(c);
                return;
            }

            // Complete reading
            state.Pop();*/
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            // Old code
            /*
            if (!_membersChecked)
            {
                _membersChecked = true;
                state.Push(new GDClassMemberResolver(this, 0));
                state.PassLineFinish();
                return;
            }

            // Complete reading
            state.Pop();*/
        }
    }
}