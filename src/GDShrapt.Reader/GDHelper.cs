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
                case GDDualOperatorType.Or: return "||";
                case GDDualOperatorType.Or2: return "or";
                case GDDualOperatorType.And: return "&&";
                case GDDualOperatorType.And2: return "and";
                case GDDualOperatorType.Is: return "is";
                case GDDualOperatorType.As: return "as";
                case GDDualOperatorType.BitShiftLeft: return "<<";
                case GDDualOperatorType.BitShiftRight: return ">>";
                case GDDualOperatorType.BitwiseOr: return "|";
                case GDDualOperatorType.BitwiseAnd: return "&";
                case GDDualOperatorType.Xor: return "^";
                case GDDualOperatorType.Mod: return "%";
                case GDDualOperatorType.ModAndAssign: return "%=";
                case GDDualOperatorType.In: return "in";
                case GDDualOperatorType.BitwiseAndAndAssign: return "&=";
                case GDDualOperatorType.BitwiseOrAndAssign: return "|=";
                case GDDualOperatorType.Power: return "**";
                case GDDualOperatorType.PowerAndAssign: return "**=";
                case GDDualOperatorType.BitShiftLeftAndAssign: return "<<=";
                case GDDualOperatorType.BitShiftRightAndAssign: return ">>=";
                case GDDualOperatorType.XorAndAssign: return "^=";
                default:
                    return "";
            }
        }

        public static string Print(this GDSingleOperatorType self)
        {
            switch (self)
            {
                case GDSingleOperatorType.Null: return "";
                case GDSingleOperatorType.Negate: return "-";
                case GDSingleOperatorType.Not: return "!";
                case GDSingleOperatorType.Not2: return "not";
                case GDSingleOperatorType.BitwiseNegate: return "~";
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
                case GDOperationType.Indexer:
                case GDOperationType.NodePath:
                case GDOperationType.DictionaryInitializer:
                case GDOperationType.ArrayInitializer:
                case GDOperationType.Member:
                case GDOperationType.Call: return 19;
                case GDOperationType.Identifier: return 21;
                case GDOperationType.Method: return 21;
                default:
                    return -1;
            }
        }

        public static int GetOperatorPriority(GDDualOperatorType type)
        {
            switch (type)
            {
                case GDDualOperatorType.Null: return 20;
                case GDDualOperatorType.Power:
                case GDDualOperatorType.Mod:
                case GDDualOperatorType.Division:
                case GDDualOperatorType.Multiply: return 14;
                case GDDualOperatorType.BitShiftLeft: 
                case GDDualOperatorType.BitShiftRight: return 12;
                case GDDualOperatorType.Subtraction:
                case GDDualOperatorType.Addition: return 13;
                case GDDualOperatorType.Equal:
                case GDDualOperatorType.NotEqual: return 10;
                case GDDualOperatorType.Assignment:
                case GDDualOperatorType.AddAndAssign:
                case GDDualOperatorType.ModAndAssign:
                case GDDualOperatorType.MultiplyAndAssign:
                case GDDualOperatorType.DivideAndAssign:
                case GDDualOperatorType.BitwiseAndAndAssign:
                case GDDualOperatorType.BitwiseOrAndAssign:
                case GDDualOperatorType.SubtractAndAssign:
                case GDDualOperatorType.PowerAndAssign:
                case GDDualOperatorType.BitShiftLeftAndAssign: 
                case GDDualOperatorType.BitShiftRightAndAssign:
                case GDDualOperatorType.XorAndAssign: return 3;
                case GDDualOperatorType.MoreThan:
                case GDDualOperatorType.LessThan:
                case GDDualOperatorType.LessThanOrEqual:
                case GDDualOperatorType.MoreThanOrEqual: return 11;
                case GDDualOperatorType.Or: 
                case GDDualOperatorType.Or2: return 5;
                case GDDualOperatorType.And:
                case GDDualOperatorType.And2: return 6;
                case GDDualOperatorType.BitwiseAnd: return 9;
                case GDDualOperatorType.Xor: return 8;
                case GDDualOperatorType.BitwiseOr: return 7;
                case GDDualOperatorType.Is:
                case GDDualOperatorType.As: return 19;
                case GDDualOperatorType.In: return 11;

                default:
                    return -1;
            }
        }

        public static int GetOperatorPriority(GDSingleOperatorType type)
        {
            switch (type)
            {
                case GDSingleOperatorType.Null: return 20;
                case GDSingleOperatorType.Negate:
                case GDSingleOperatorType.Not:
                case GDSingleOperatorType.Not2:
                case GDSingleOperatorType.BitwiseNegate: return 16;
                default:
                    return -1;
            };
        }

        public static GDAssociationOrderType GetOperatorAssociationOrder(GDDualOperatorType type)
        {
            switch (type)
            {
                case GDDualOperatorType.Null: 
                    return GDAssociationOrderType.Undefined;
                case GDDualOperatorType.Power:
                    return GDAssociationOrderType.FromLeftToRight;
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
                case GDDualOperatorType.Or2:
                case GDDualOperatorType.And:
                case GDDualOperatorType.And2:
                case GDDualOperatorType.In:
                case GDDualOperatorType.Xor:
                case GDDualOperatorType.BitwiseOr:
                case GDDualOperatorType.BitwiseAnd:
                case GDDualOperatorType.BitShiftLeft:
                case GDDualOperatorType.BitShiftRight:
                case GDDualOperatorType.Mod:
                    return GDAssociationOrderType.FromLeftToRight;

                case GDDualOperatorType.Assignment:
                case GDDualOperatorType.AddAndAssign:
                case GDDualOperatorType.MultiplyAndAssign:
                case GDDualOperatorType.SubtractAndAssign:
                case GDDualOperatorType.DivideAndAssign:
                case GDDualOperatorType.ModAndAssign:
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
