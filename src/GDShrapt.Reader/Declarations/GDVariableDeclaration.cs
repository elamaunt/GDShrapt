using System;

namespace GDShrapt.Reader
{
    public sealed class GDVariableDeclaration : GDClassMember
    {
        bool _setgetKeywordChecked;
        bool _hasSetget;

        public GDIdentifier Identifier { get; set; }
        public GDType Type { get; set; }
        public GDExpression Initializer { get; set; }

        public GDIdentifier GetMethodIdentifier { get; set; }
        public GDIdentifier SetMethodIdentifier { get; set; }

        public bool IsExported { get; set; }
        public bool IsConstant { get; set; }
        public bool HasOnReadyInitialization { get; set; }

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
                state.PassChar(c);
                return;
            }

            if (c == '=')
            {
                state.Push(new GDExpressionResolver(this));
                return;
            }

            if (!IsConstant && !_setgetKeywordChecked)
            {
                _setgetKeywordChecked = true;
                state.Push(new GDStaticKeywordResolver(this));
                state.PassChar(c);
                return;
            }

            if (_hasSetget)
            {
                if (SetMethodIdentifier == null)
                {
                    state.Push(SetMethodIdentifier = new GDIdentifier());
                    state.PassChar(c);
                    return;
                }

                if (GetMethodIdentifier == null)
                {
                    state.Push(GetMethodIdentifier = new GDIdentifier());

                    if (c != ',')
                        state.PassChar(c);
                    return;
                }
            }

            state.Pop();
            state.PassChar(c);
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.Pop();
            state.PassLineFinish();
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