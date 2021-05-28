namespace GDSharp.Reader
{
    public static class GDHelper
    {
        public static string Print(this GDDualOperatorType self)
        {
            switch (self)
            {
                case GDDualOperatorType.Null: return "";
                case GDDualOperatorType.Unknown: return "";
                case GDDualOperatorType.MoreThan: return ">";
                case GDDualOperatorType.LessThan: return "<";
                case GDDualOperatorType.Assignment: return "=";
                case GDDualOperatorType.Subtraction: return "-";
                case GDDualOperatorType.Division: return "/";
                case GDDualOperatorType.Multiply: return "*";
                case GDDualOperatorType.Addition: return "+";
                case GDDualOperatorType.AddAndAssign: return "+=";
                case GDDualOperatorType.NotEqual: return "!=";
                case GDDualOperatorType.MultiplyAndAssign: return "*=";
                case GDDualOperatorType.SubtractAndAssign: return "-=";
                case GDDualOperatorType.LessThanOrEqual: return "<=";
                case GDDualOperatorType.MoreThanOrEqual: return ">=";
                case GDDualOperatorType.Equal: return "==";
                case GDDualOperatorType.DivideAndAssign: return "/=";
                case GDDualOperatorType.Or: return "or";
                case GDDualOperatorType.And: return "and";
                case GDDualOperatorType.Is: return "is";
                case GDDualOperatorType.As: return "as";
                default: return "";
            }
        }

        public static string Print(this GDSingleOperatorType self)
        {
            switch (self)
            {
                case GDSingleOperatorType.Null: return "";
                case GDSingleOperatorType.Unknown: return "";
                case GDSingleOperatorType.Negate: return "-";
                case GDSingleOperatorType.Not: return "!";
                default: return "";
            }
        }
    }
}
