using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public class GDEnumDeclaration : GDClassMember
    {
        bool _nameSkiped;
        public GDIdentifier Identifier { get; set; }
        public List<GDEnumValueDeclaration> Values { get; } = new List<GDEnumValueDeclaration>();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == '}')
            {
                state.PopNode();
                return;
            }

            if (c == '{')
            {
                _nameSkiped = true;
                return;
            }

            if (!_nameSkiped)
            {
                if (Identifier == null)
                {
                    state.PushNode(Identifier = new GDIdentifier());
                    state.PassChar(c);
                    return;
                }
            }

            var decl = new GDEnumValueDeclaration();
            Values.Add(decl);
            state.PushNode(decl);

            if (c != ',')
                state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }

        public override string ToString()
        {
            if (Identifier != null)
            {
                if (Values.Count == 0)
                    return $"enum {Identifier} {{}}";

                return $"enum {Identifier} {{{string.Join(", ", Values.Select(x => x.ToString()))}}}";
            }
            else
            {
                if (Values.Count == 0)
                    return $"enum {{}}";

                return $"enum {{{string.Join(", ", Values.Select(x => x.ToString()))}}}";
            }
        }
    }
}
