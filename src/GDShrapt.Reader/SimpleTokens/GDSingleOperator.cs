namespace GDShrapt.Reader
{
    public sealed class GDSingleOperator : GDSimpleSyntaxToken
    {
        public GDSingleOperatorType OperatorType { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            state.Pop();
            state.PassChar(c);
        }

        public override GDSyntaxToken Clone()
        {
            return new GDSingleOperator()
            {
                OperatorType = OperatorType
            };
        }

        public override string ToString()
        {
            return OperatorType.Print();
        }
    }
}
