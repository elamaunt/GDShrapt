using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDMethodDeclaration : GDClassMember
    {
        public GDIdentifier Identifier { get; set; }
        public GDParameters Parameters { get; set; }
        public GDType ReturnType { get; set; }

        public bool IsStatic { get; set; }

        public List<GDMethodStatement> Statements { get; } = new List<GDMethodStatement>();

        public override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Identifier == null)
            {
                state.PushNode(Identifier = new GDIdentifier());
                state.HandleChar(c);
                return;
            }

            if (Parameters == null)
            {
                state.PushNode(Parameters = new GDParameters());
                state.HandleChar(c);
                return;
            }

            if (c == ':')
            {
                state.PushNode(new GDMethodStatementResolver(this));
            }
            else
            {
                if (c == '-' || c == '>')
                    return;

                state.PushNode(ReturnType = new GDType());
            }
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            // Nothing
        }
    }
}