using System;

namespace GDShrapt.Reader
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
                default: 
                    return "";
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
                default:
                    return "";
            }
        }

        public static int GetOperationPriority(GDOperationType type)
        {
            switch (type)
            {
                case GDOperationType.Literal: return 21;
                case GDOperationType.Brackets: return 20;
                case GDOperationType.Member:
                case GDOperationType.Call: return 19;
                case GDOperationType.Identifier: return 21;
                default:
                    return -1;
            }
        }

        public static int GetOperatorPriority(GDDualOperatorType type)
        {
            switch (type)
            {
                case GDDualOperatorType.Null:
                case GDDualOperatorType.Unknown: return 20;
                case GDDualOperatorType.Division:
                case GDDualOperatorType.Multiply: return 14;
                case GDDualOperatorType.Subtraction:
                case GDDualOperatorType.Addition: return 13;
                case GDDualOperatorType.Equal:
                case GDDualOperatorType.NotEqual: return 10;
                case GDDualOperatorType.Assignment:
                case GDDualOperatorType.AddAndAssign:
                case GDDualOperatorType.MultiplyAndAssign:
                case GDDualOperatorType.DivideAndAssign:
                case GDDualOperatorType.SubtractAndAssign: return 3;
                case GDDualOperatorType.MoreThan:
                case GDDualOperatorType.LessThan:
                case GDDualOperatorType.LessThanOrEqual:
                case GDDualOperatorType.MoreThanOrEqual: return 11;
                case GDDualOperatorType.Or: return 5;
                case GDDualOperatorType.And: return 6;
                case GDDualOperatorType.Is:
                case GDDualOperatorType.As: return 19;
                default:
                    return -1;
            }
        }

        public static int GetOperatorPriority(GDSingleOperatorType type)
        {
            switch (type)
            {
                case GDSingleOperatorType.Null:
                case GDSingleOperatorType.Unknown: return 20;
                case GDSingleOperatorType.Negate: return 16;
                case GDSingleOperatorType.Not: return 16;
                default:
                    return -1;
            };
        }

        public static GDAssociationOrderType GetOperatorAssociationOrder(GDDualOperatorType type)
        {
            switch (type)
            {
                case GDDualOperatorType.Null: 
                case GDDualOperatorType.Unknown:
                    return GDAssociationOrderType.Undefined;

                case GDDualOperatorType.MoreThan:
                case GDDualOperatorType.LessThan:
                case GDDualOperatorType.Subtraction:
                case GDDualOperatorType.Division:
                case GDDualOperatorType.Multiply:
                case GDDualOperatorType.Addition:
                case GDDualOperatorType.NotEqual:
                case GDDualOperatorType.LessThanOrEqual:
                case GDDualOperatorType.MoreThanOrEqual:
                case GDDualOperatorType.Equal:
                case GDDualOperatorType.Or:
                case GDDualOperatorType.And:
                    return GDAssociationOrderType.FromLeftToRight;

                case GDDualOperatorType.Assignment:
                case GDDualOperatorType.AddAndAssign:
                case GDDualOperatorType.MultiplyAndAssign:
                case GDDualOperatorType.SubtractAndAssign:
                case GDDualOperatorType.DivideAndAssign:
                    return GDAssociationOrderType.FromRightToLeft;

                case GDDualOperatorType.Is:
                case GDDualOperatorType.As:
                    return GDAssociationOrderType.Undefined;

                default:
                    return GDAssociationOrderType.Undefined;
            }
        }
    }
}
