namespace GDScriptConverter
{
    public class GDVariableDeclaration : GDStatement
    {
        public GDNameIdentifier Identifier { get; private set; }
        public GDType Type { get; private set; }
        public GDExpression Initializer { get; private set; }

        public override void HandleChar(char c, GDReadingState state)
        {
            
        }

        public override void HandleLineFinish(GDReadingState state)
        {
            
        }
    }
}