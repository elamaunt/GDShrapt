using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    public sealed class GDDictionaryInitializerExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.DictionaryInitializer);
        public List<GDDictionaryKeyValueDeclaration> KeyValues { get; } = new List<GDDictionaryKeyValueDeclaration>();

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (c == '}')
            {
                state.Pop();
                return;
            }

            var decl = new GDDictionaryKeyValueDeclaration();
            KeyValues.Add(decl);
            state.Push(decl);

            if (c != ',')
                state.PassChar(c);
        }

        internal override void HandleNewLineChar(GDReadingState state)
        {
            state.Pop();
            state.PassNewLine();
        }

        public override string ToString()
        {
            return $"{{{string.Join(", ", KeyValues.Select(x => x.ToString()))}}}";
        }
    }
}
