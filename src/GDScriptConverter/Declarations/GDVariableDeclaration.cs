using System;

namespace GDScriptConverter
{
    public class GDVariableDeclaration : GDClassMember
    {
        public GDIdentifier Identifier { get; set; }
        public GDType Type { get; set; }
        public GDExpression Initializer { get; set; }

        public bool IsConstant { get; set; }

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

            if (Type == null && c == ':')
            {
                state.PushNode(Type = new GDType());
                state.HandleChar(c);
                return;
            }

            if (c == '=')
            {
                state.PushNode(new GDExpressionResolver(expr => Initializer = expr));
            }
            else
            {
                // Consider another characters in line as a comment
                state.PushNode(new GDComment());
                state.HandleChar(c);
            }
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
        }
        public override string ToString()
        {
            if (Type != null)
            {
                var type = Type.ToString();

                if (type.IsNullOrEmpty())
                    return $"{(IsConstant ? "const" : "var")} {Identifier} := {Initializer}";
                return $"{(IsConstant ? "const" : "var")} {Identifier} : {type} = {Initializer}";
            }
            return $"{(IsConstant ? "const" : "var")} {Identifier} = {Initializer}";
        }
    }
}