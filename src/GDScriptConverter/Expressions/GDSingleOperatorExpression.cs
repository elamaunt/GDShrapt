namespace GDScriptConverter
{
    public class GDSingleOperatorExpression : GDExpression
    {
        public GDSingleOperatorType Type { get; set; }
        public GDExpression TargetExpression { get; set; }

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
                return;

            if (Type == GDSingleOperatorType.Unknown)
            {
                if (c == '-')
                {
                    Type = GDSingleOperatorType.Negate;
                    return;
                }

                // TODO: is it in GD?
                /*
                if (c == '!')
                {
                    Type = GDSingleOperatorType.Not;
                    return;
                }
                */
            }
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            throw new System.NotImplementedException();
        }
    }
}
