using System;

namespace GDScriptConverter
{
    public class GDSingleOperator : GDPattern
    {




        protected internal override void HandleChar(char c, GDReadingState state)
        {
            throw new NotImplementedException();
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            throw new NotImplementedException();
        }

        public static int GetOperatorPriority(GDSingleOperatorType type)
        {
            switch (type)
            {
                case GDSingleOperatorType.Unknown: return 0;
                case GDSingleOperatorType.Negate: return 1;
                case GDSingleOperatorType.Not: return 2;
                default: return 0;
            }
        }
    }
}
