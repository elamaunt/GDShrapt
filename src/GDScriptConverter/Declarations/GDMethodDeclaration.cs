using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDMethodDeclaration : GDClassMember
    {
        public GDIdentifier Identifier { get; set; }
        public GDParametersDeclaration Parameters { get; set; }
        public GDType ReturnType { get; set; }

        public bool IsStatic { get; set; }

        public List<GDStatement> Statements { get; } = new List<GDStatement>();

        protected internal override void HandleChar(char c, GDReadingState state)
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
                state.PushNode(Parameters = new GDParametersDeclaration());
                state.HandleChar(c);
                return;
            }

            if (c == ':')
            {
                state.PushNode(new GDStatementResolver(expr => Statements.Add(expr)));
            }
            else
            {
                if (c == '-' || c == '>')
                    return;

                state.PushNode(ReturnType = new GDType());
            }
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            // Nothing
        }
    }
}