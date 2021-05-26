using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GDScriptConverter
{
    public class GDClassDeclaration : GDTypeDeclaration
    {
        public List<GDClassMember> Members { get; } = new List<GDClassMember>();

        public GDType ExtendsClass => Members.OfType<GDExtendsAtribute>().FirstOrDefault()?.Type;
        public GDIdentifier Name => Members.OfType<GDClassNameAtribute>().FirstOrDefault()?.Identifier;
        public bool IsTool => Members.OfType<GDToolAtribute>().Any();

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            state.PushNode(new GDClassMemberResolver(this));
            state.HandleChar(c);
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            // Nothing
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