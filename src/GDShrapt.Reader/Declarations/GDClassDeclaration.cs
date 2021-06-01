using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader
{
    public class GDClassDeclaration : GDNode
    {
        bool _membersChecked;

        public List<GDClassMember> Members { get; } = new List<GDClassMember>();

        public GDExtendsAtribute Extends => Members.OfType<GDExtendsAtribute>().FirstOrDefault();
        public GDClassNameAtribute ClassName => Members.OfType<GDClassNameAtribute>().FirstOrDefault();
        public bool IsTool => Members.OfType<GDToolAtribute>().Any();
        public IEnumerable<GDMethodDeclaration> Methods => Members.OfType<GDMethodDeclaration>();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (!_membersChecked)
            {
                _membersChecked = true;
                state.PushNode(new GDClassMemberResolver(0, member => Members.Add(member)));
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
                state.PushNode(new GDClassMemberResolver(0, member => Members.Add(member)));
                state.PassLineFinish();
                return;
            }

            // Complete reading
            state.PopNode();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            for (int i = 0; i < Members.Count; i++)
                builder.AppendLine(Members[i].ToString());

            return builder.ToString();
        }
    }
}