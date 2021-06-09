namespace GDShrapt.Reader
{
    public sealed class GDDualOperator : GDSimpleSyntaxToken
    {
        public GDDualOperatorType OperatorType { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.Pop();
            state.PassChar(c);
        }

        public override string ToString()
        {
            return OperatorType.Print();
        }
    }
}
