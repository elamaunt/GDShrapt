using System;

namespace GDScriptConverter
{
    public class GDDualOperator : GDPattern
    {


        protected internal override void HandleChar(char c, GDReadingState state)
        {
            throw new NotImplementedException();
        }

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            throw new NotImplementedException();
        }

        public static int GetOperatorPriority(GDDualOperatorType type)
        {
            switch (type)
            {
                case GDDualOperatorType.Unknown: return 0;
                case GDDualOperatorType.And: return 0;
                case GDDualOperatorType.Or: return 1;
                case GDDualOperatorType.Equal: return 2;
                case GDDualOperatorType.Is: return 2;
                case GDDualOperatorType.MoreThan: return 2;
                case GDDualOperatorType.LessThan: return 2;
                case GDDualOperatorType.Assignment: return 2;
                case GDDualOperatorType.Subtraction: return 3;
                case GDDualOperatorType.Division: return 4;
                case GDDualOperatorType.Multiply: return 4;
                case GDDualOperatorType.Addition: return 3;
                case GDDualOperatorType.AddAndAssign: return 2;
                case GDDualOperatorType.NotEqual: return 2;
                case GDDualOperatorType.MultiplyAndAssign: return 2;
                case GDDualOperatorType.SubtractAndAssign: return 2;
                case GDDualOperatorType.LessThanOrEqual: return 2;
                case GDDualOperatorType.MoreThanOrEqual: return 2;
                case GDDualOperatorType.DivideAndAssign: return 2;
                
                default: return 0;
            }
        }
    }
}
