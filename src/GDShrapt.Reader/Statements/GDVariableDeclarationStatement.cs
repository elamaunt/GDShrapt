using System;

namespace GDShrapt.Reader
{
    public sealed class GDVariableDeclarationStatement : GDStatement
    {
        public GDIdentifier Identifier { get; set; }
        public GDType Type { get; set; }
        public GDExpression Initializer { get; set; }
        public bool IsConstant { get; set; }

        internal GDVariableDeclarationStatement(int lineIntendation)
            : base(lineIntendation)
        {
        }

        public GDVariableDeclarationStatement()
        {

        }

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Identifier == null)
            {
                state.SetReadingToken(Identifier = new GDIdentifier());
                state.PassChar(c);
                return;
            }

            if (Type == null && c == ':')
            {
                state.SetReadingToken(Type = new GDType());
                return;
            }

            if (c == '=')
            {
                state.PushNode(new GDExpressionResolver(expr => Initializer = expr));
                return;
            }

            // TODO: handle setget 
        }

        internal override void HandleLineFinish(GDReadingState state)
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