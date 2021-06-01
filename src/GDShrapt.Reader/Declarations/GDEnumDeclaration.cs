using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public class GDEnumDeclaration : GDClassMember
    {
        public GDIdentifier Identifier { get; set; }
        public List<GDEnumValueDeclaration> Values { get; } = new List<GDEnumValueDeclaration>();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Identifier == null)
            {
                state.PushNode(Identifier = new GDIdentifier());
                state.PassChar(c);
                return;
            }



           /* if (c == '}')
            {
                state.PopNode();
            }
            else
            {
                if (c == ',' || c == '{')
                {
                    var value = new GDEnumValueDeclaration();
                    Values.Add(value);
                    state.PushNode(value);
                    return;
                }
            }

            state.PopNode();
            state.PassChar(c);*/
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
           // state.PopNode();
           // state.PassChar(c);
        }
    }
}
