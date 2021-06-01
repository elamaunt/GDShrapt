namespace GDShrapt.Reader
{
    public class GDEnumValueDeclaration : GDNode
    {
        public GDIdentifier Identifier { get; set; }
        public GDExpression Value { get; set; }

        internal override void HandleChar(char c, GDReadingState state)
        {
            throw new System.NotImplementedException();
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}