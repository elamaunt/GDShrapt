using System;

namespace GDShrapt.Reader
{
    public static partial class GDBuildingExtensionMethods
    {
        public static T AddIdentifierExpression<T>(this T receiver, string identifier)
           where T : ITokenReceiver<GDIdentifierExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Identifier(identifier));
            return receiver;
        }

        public static T AddStringExpression<T>(this T receiver, string value, bool multiline = false, GDStringBoundingChar boundingChar = GDStringBoundingChar.DoubleQuotas)
           where T : ITokenReceiver<GDStringExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.String(value, multiline, boundingChar));
            return receiver;
        }

        public static T AddNumberExpression<T>(this T receiver, string value)
           where T : ITokenReceiver<GDNumberExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Number(value));
            return receiver;
        }

        public static T AddNumberExpression<T>(this T receiver, int value)
           where T : ITokenReceiver<GDNumberExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Number(value));
            return receiver;
        }

        public static T AddNumberExpression<T>(this T receiver, long value)
           where T : ITokenReceiver<GDNumberExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Number(value));
            return receiver;
        }

        public static T AddNumberExpression<T>(this T receiver, double value)
           where T : ITokenReceiver<GDNumberExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Number(value));
            return receiver;
        }

        public static T AddIfExpression<T>(this T receiver, GDExpression condition, GDExpression trueExpr, GDExpression falseExpr)
           where T : ITokenReceiver<GDIfExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.If(condition, trueExpr, falseExpr));
            return receiver;
        }

        public static T AddIfExpression<T>(this T receiver, Func<GDIfExpression, GDIfExpression> setup)
           where T : ITokenReceiver<GDIfExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.If(setup));
            return receiver;
        }

        public static T AddArrayExpression<T>(this T receiver, params GDExpression[] expressions)
           where T : ITokenReceiver<GDArrayInitializerExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Array(expressions));
            return receiver;
        }

        public static T AddArrayExpression<T>(this T receiver, Func<GDArrayInitializerExpression, GDArrayInitializerExpression> setup)
           where T : ITokenReceiver<GDArrayInitializerExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Array(setup));
            return receiver;
        }

        public static T AddDictionaryExpression<T>(this T receiver, params GDDictionaryKeyValueDeclaration[] keyValues)
           where T : ITokenReceiver<GDDictionaryInitializerExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Dictionary(keyValues));
            return receiver;
        }

        public static T AddDictionaryExpression<T>(this T receiver, Func<GDDictionaryInitializerExpression, GDDictionaryInitializerExpression> setup)
           where T : ITokenReceiver<GDDictionaryInitializerExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Dictionary(setup));
            return receiver;
        }

        public static T AddTrueExpression<T>(this T receiver)
           where T : ITokenReceiver<GDBoolExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.True());
            return receiver;
        }

        public static T AddFalseExpression<T>(this T receiver)
           where T : ITokenReceiver<GDBoolExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.False());
            return receiver;
        }

        public static T AddCallExpression<T>(this T receiver, GDExpression caller, params GDExpression[] parameters)
           where T : ITokenReceiver<GDCallExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Call(caller, parameters));
            return receiver;
        }

        public static T AddCallExpression<T>(this T receiver, Func<GDCallExpression, GDCallExpression> setup)
           where T : ITokenReceiver<GDCallExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Call(setup));
            return receiver;
        }

        public static T AddBracketExpression<T>(this T receiver, Func<GDBracketExpression, GDBracketExpression> setup)
           where T : ITokenReceiver<GDBracketExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Bracket(setup));
            return receiver;
        }

        public static T AddBracketExpression<T>(this T receiver, GDExpression inner)
           where T : ITokenReceiver<GDBracketExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Bracket(inner));
            return receiver;
        }

        public static T AddMemberOperatorExpression<T>(this T receiver, Func<GDMemberOperatorExpression, GDMemberOperatorExpression> setup)
           where T : ITokenReceiver<GDMemberOperatorExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Member(setup));
            return receiver;
        }

        public static T AddMemberOperatorExpression<T>(this T receiver, GDExpression caller, string identifier)
           where T : ITokenReceiver<GDMemberOperatorExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Member(caller, identifier));
            return receiver;
        }

        public static T AddMemberOperatorExpression<T>(this T receiver, string identifier)
           where T : ITokenReceiver<GDMemberOperatorExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.BaseMember(identifier));
            return receiver;
        }

        public static T AddIndexerExpression<T>(this T receiver, GDExpression caller, GDExpression indexExpression)
          where T : ITokenReceiver<GDIndexerExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Indexer(caller, indexExpression));
            return receiver;
        }

        public static T AddIndexerExpression<T>(this T receiver, Func<GDIndexerExpression, GDIndexerExpression> setup)
          where T : ITokenReceiver<GDIndexerExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Indexer(setup));
            return receiver;
        }

        public static T AddPassExpression<T>(this T receiver)
          where T : ITokenReceiver<GDPassExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Pass());
            return receiver;
        }

        public static T AddBreakPointExpression<T>(this T receiver)
          where T : ITokenReceiver<GDBreakPointExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.BreakPoint());
            return receiver;
        }

        public static T AddBreakExpression<T>(this T receiver)
          where T : ITokenReceiver<GDBreakExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Break());
            return receiver;
        }

        public static T AddContinueExpression<T>(this T receiver)
          where T : ITokenReceiver<GDContinueExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Continue());
            return receiver;
        }

        public static T AddReturnExpression<T>(this T receiver)
          where T : ITokenReceiver<GDReturnExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Return());
            return receiver;
        }

        public static T AddReturnExpression<T>(this T receiver, Func<GDReturnExpression, GDReturnExpression> setup)
          where T : ITokenReceiver<GDReturnExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Return(setup));
            return receiver;
        }

        public static T AddReturnExpression<T>(this T receiver, GDExpression result)
          where T : ITokenReceiver<GDReturnExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Return(result));
            return receiver;
        }

        public static T AddDualOperatorExpression<T>(this T receiver, GDExpression left, GDDualOperator @operator, GDExpression right)
          where T : ITokenReceiver<GDDualOperatorExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.DualOperator(left, @operator, right));
            return receiver;
        }

        public static T AddDualOperatorExpression<T>(this T receiver, Func<GDDualOperatorExpression, GDDualOperatorExpression> setup)
          where T : ITokenReceiver<GDDualOperatorExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.DualOperator(setup));
            return receiver;
        }

        public static T AddSingleOperatorExpression<T>(this T receiver, Func<GDSingleOperatorExpression, GDSingleOperatorExpression> setup)
          where T : ITokenReceiver<GDSingleOperatorExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.SingleOperator(setup));
            return receiver;
        }

        public static T AddSingleOperator<T>(this T receiver, GDSingleOperator @operator, GDExpression operand)
         where T : ITokenReceiver<GDSingleOperatorExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.SingleOperator(@operator, operand));
            return receiver;
        }

        public static T AddGetNodeExpression<T>(this T receiver, Func<GDGetNodeExpression, GDGetNodeExpression> setup)
         where T : ITokenReceiver<GDGetNodeExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.GetNode(setup));
            return receiver;
        }

        public static T AddGetNodeExpression<T>(this T receiver, GDPathList pathList)
         where T : ITokenReceiver<GDGetNodeExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.GetNode(pathList));
            return receiver;
        }

        public static T AddGetNodeExpression<T>(this T receiver, params GDIdentifier[] names)
         where T : ITokenReceiver<GDGetNodeExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.GetNode(names));
            return receiver;
        }

        public static T AddNodePathExpression<T>(this T receiver, string path)
         where T : ITokenReceiver<GDNodePathExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.NodePath(path));
            return receiver;
        }

        public static T AddNodePathExpression<T>(this T receiver, Func<GDNodePathExpression, GDNodePathExpression> setup)
         where T : ITokenReceiver<GDNodePathExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.NodePath(setup));
            return receiver;
        }

        public static T AddMatchCaseVariableExpression<T>(this T receiver, Func<GDMatchCaseVariableExpression, GDMatchCaseVariableExpression> setup)
         where T : ITokenReceiver<GDMatchCaseVariableExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.MatchCaseVariable(setup));
            return receiver;
        }

        public static T AddMatchCaseVariableExpression<T>(this T receiver, string identifier)
         where T : ITokenReceiver<GDMatchCaseVariableExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.MatchCaseVariable(identifier));
            return receiver;
        }

        public static T AddYieldExpression<T>(this T receiver, Func<GDYieldExpression, GDYieldExpression> setup)
         where T : ITokenReceiver<GDYieldExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Yield(setup));
            return receiver;
        }

        public static T AddYieldExpression<T>(this T receiver, params GDExpression[] parameters)
         where T : ITokenReceiver<GDYieldExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.Yield(parameters));
            return receiver;
        }

        public static T AddMatchDefaultOperator<T>(this T receiver)
         where T : ITokenReceiver<GDMatchDefaultOperatorExpression>
        {
            receiver.HandleReceivedToken(GD.Expression.MatchDefaultOperator());
            return receiver;
        }
    }
}
