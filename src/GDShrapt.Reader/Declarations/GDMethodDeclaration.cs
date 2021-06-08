using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public sealed class GDMethodDeclaration : GDClassMember
    {
        bool _statementsChecked;
        bool _typeChecked;
        public GDIdentifier Identifier { get; set; }
        public GDParametersDeclaration Parameters { get; set; }
        public GDType ReturnType { get; set; }

        public bool IsStatic { get; set; }

        public List<GDStatement> Statements { get; } = new List<GDStatement>();

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

            if (Parameters == null)
            {
                state.PushNode(Parameters = new GDParametersDeclaration());
                state.PassChar(c);
                return;
            }

            if (_statementsChecked)
            {
                state.PopNode();
                state.PassChar(c);
                return;
            }

            if (c == ':')
            {
                _typeChecked = true;
                _statementsChecked = true;
                state.PushNode(new GDStatementResolver(1, expr => Statements.Add(expr)));
            }
            else
            {
                if (!_typeChecked)
                {
                    if (c == '-' || c == '>')
                        return;

                    state.SetReadingToken(ReturnType = new GDType());
                    state.PassChar(c);
                    _typeChecked = true;
                    return;
                }

                state.PopNode();
                state.PassChar(c);
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            state.PopNode();
            state.PassLineFinish();
        }
    }
}