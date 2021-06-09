namespace GDShrapt.Reader
{
    public sealed class GDVariableDeclarationExpression : GDExpression
    {
        public override int Priority => GDHelper.GetOperationPriority(GDOperationType.VariableDeclaration);
        public GDIdentifier Identifier { get; set; }

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
            return $"var {Identifier}";
        }
    }
}
