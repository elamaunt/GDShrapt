using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    public sealed class GDClassDeclaration : GDNode
    {
        bool _membersChecked;

        public IEnumerable<GDClassMember> Members => TokensList.OfType<GDClassMember>();

        public GDExtendsAtribute Extends => TokensList.OfType<GDExtendsAtribute>().FirstOrDefault();
        public GDClassNameAtribute ClassName => TokensList.OfType<GDClassNameAtribute>().FirstOrDefault();
        public bool IsTool => Members.OfType<GDToolAtribute>().Any();

        public IEnumerable<GDVariableDeclaration> Variables => TokensList.OfType<GDVariableDeclaration>();
        public IEnumerable<GDMethodDeclaration> Methods => TokensList.OfType<GDMethodDeclaration>();
        public IEnumerable<GDEnumDeclaration> Enums => TokensList.OfType<GDEnumDeclaration>();
        public IEnumerable<GDInnerClassDeclaration> InnerClasses => TokensList.OfType<GDInnerClassDeclaration>();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_membersChecked)
            {
                _membersChecked = true;
                state.PushNode(new GDClassMemberResolver(0, member => TokensList.AddLast(member)));
                state.PassChar(c);
                return;
            }

            // Complete reading
            state.PopNode();
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            if (!_membersChecked)
            {
                _membersChecked = true;
                state.PushNode(new GDClassMemberResolver(0, member => TokensList.AddLast(member)));
                state.PassLineFinish();
                return;
            }

            // Complete reading
            state.PopNode();
        }
    }
}