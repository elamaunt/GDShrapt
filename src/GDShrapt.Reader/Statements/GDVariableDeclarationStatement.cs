using System;

namespace GDShrapt.Reader
{
    public sealed class GDVariableDeclarationStatement : GDStatement, IExpressionsReceiver
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
                state.Push(Identifier = new GDIdentifier());
                state.PassChar(c);
                return;
            }

            if (Type == null && c == ':')
            {
                state.Push(Type = new GDType());
                return;
            }

            if (c == '=')
            {
                state.Push(new GDExpressionResolver(this));
                return;
            }

            // TODO: handle setget 
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
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

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            throw new NotImplementedException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            throw new NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDComment token)
        {
            throw new NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            throw new NotImplementedException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDSpace token)
        {
            throw new NotImplementedException();
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
            throw new NotImplementedException();
        }
    }
}