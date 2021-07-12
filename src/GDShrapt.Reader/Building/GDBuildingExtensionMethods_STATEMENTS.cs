using System;

namespace GDShrapt.Reader
{
    public static partial class GDBuildingExtensionMethods
    {
        public static T AddFor<T>(this T receiver, Func<GDForStatement, GDForStatement> setup)
           where T : ITokenReceiver<GDForStatement>
        {
            receiver.HandleReceivedToken(GD.Statement.For(setup));
            return receiver;
        }

        public static T AddFor<T>(this T receiver, GDIdentifier variable, GDExpression collection, GDExpression body)
           where T : ITokenReceiver<GDForStatement>
        {
            receiver.HandleReceivedToken(GD.Statement.For(variable, collection, body));
            return receiver;
        }

        public static T AddFor<T>(this T receiver, GDIdentifier variable, GDExpression collection, params GDStatement[] statements)
           where T : ITokenReceiver<GDForStatement>
        {
            receiver.HandleReceivedToken(GD.Statement.For(variable, collection, statements));
            return receiver;
        }

        public static T AddExpression<T>(this T receiver, GDExpression expression)
          where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Statement.Expression(expression));
            return receiver;
        }

        public static T AddIf<T>(this T receiver, Func<GDIfStatement, GDIfStatement> setup)
          where T : ITokenReceiver<GDIfStatement>
        {
            receiver.HandleReceivedToken(GD.Statement.If(setup));
            return receiver;
        }

        public static T AddIf<T>(this T receiver, GDIfBranch ifBranch)
          where T : ITokenReceiver<GDIfStatement>
        {
            receiver.HandleReceivedToken(GD.Statement.If(ifBranch));
            return receiver;
        }

        public static T AddIf<T>(this T receiver, GDIfBranch ifBranch, GDElseBranch elseBranch)
          where T : ITokenReceiver<GDIfStatement>
        {
            receiver.HandleReceivedToken(GD.Statement.If(ifBranch, elseBranch));
            return receiver;
        }

        public static T AddIf<T>(this T receiver, GDIfBranch ifBranch, GDElifBranchesList elifBranches)
          where T : ITokenReceiver<GDIfStatement>
        {
            receiver.HandleReceivedToken(GD.Statement.If(ifBranch, elifBranches));
            return receiver;
        }

        public static T AddIf<T>(this T receiver, GDIfBranch ifBranch, params GDElifBranch[] elifBranches)
          where T : ITokenReceiver<GDIfStatement>
        {
            receiver.HandleReceivedToken(GD.Statement.If(ifBranch, elifBranches));
            return receiver;
        }

        public static T AddIf<T>(this T receiver, GDIfBranch ifBranch, GDElifBranchesList elifBranches, GDElseBranch elseBranch)
          where T : ITokenReceiver<GDIfStatement>
        {
            receiver.HandleReceivedToken(GD.Statement.If(ifBranch, elifBranches, elseBranch));
            return receiver;
        }

        public static T AddYield<T>(this T receiver, Func<GDYieldExpression, GDYieldExpression> setup)
         where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Expression.Yield(setup));
            return receiver;
        }

        public static T AddYield<T>(this T receiver, params GDExpression[] parameters)
         where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Expression.Yield(parameters));
            return receiver;
        }

        public static T AddPass<T>(this T receiver)
         where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Expression.Pass());
            return receiver;
        }

        public static T AddReturn<T>(this T receiver)
          where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Expression.Return());
            return receiver;
        }

        public static T AddReturn<T>(this T receiver, Func<GDReturnExpression, GDReturnExpression> setup)
          where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Expression.Return(setup));
            return receiver;
        }

        public static T AddReturn<T>(this T receiver, GDExpression result)
          where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Expression.Return(result));
            return receiver;
        }

        public static T AddDualOperator<T>(this T receiver, GDExpression left, GDDualOperator @operator, GDExpression right)
          where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Expression.DualOperator(left, @operator, right));
            return receiver;
        }

        public static T AddDualOperator<T>(this T receiver, Func<GDDualOperatorExpression, GDDualOperatorExpression> setup)
          where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Expression.DualOperator(setup));
            return receiver;
        }

        public static T AddCall<T>(this T receiver, GDExpression caller, params GDExpression[] parameters)
           where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Expression.Call(caller, parameters));
            return receiver;
        }

        public static T AddCall<T>(this T receiver, Func<GDCallExpression, GDCallExpression> setup)
           where T : ITokenReceiver<GDExpressionStatement>
        {
            receiver.HandleReceivedToken(GD.Expression.Call(setup));
            return receiver;
        }
    }
}
