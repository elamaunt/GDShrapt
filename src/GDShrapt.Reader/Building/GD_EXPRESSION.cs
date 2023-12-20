using System;

namespace GDShrapt.Reader
{
    public static partial class GD
    {
        public static class Expression
        {
            public static GDIdentifierExpression Identifier(string name) => new GDIdentifierExpression() { Identifier = Syntax.Identifier(name) };
            public static GDIdentifierExpression Identifier(GDIdentifier identifier) => new GDIdentifierExpression() { Identifier = identifier };
           
            public static GDStringExpression String(string value, GDStringBoundingChar boundingChar = GDStringBoundingChar.DoubleQuotas) => new GDStringExpression()
            {
                String = Syntax.String(value, boundingChar)
            };

            public static GDNumberExpression Number(string value) => new GDNumberExpression() { Number = Syntax.Number(value) };
            public static GDNumberExpression Number(long value) => new GDNumberExpression() { Number = Syntax.Number(value) };
            public static GDNumberExpression Number(double value) => new GDNumberExpression() { Number = Syntax.Number(value) };

            public static GDIfExpression If() => new GDIfExpression();
            public static GDIfExpression If(Func<GDIfExpression, GDIfExpression> setup) => setup(new GDIfExpression());
            public static GDIfExpression If(params GDSyntaxToken[] unsafeTokens) => new GDIfExpression() { FormTokensSetter = unsafeTokens };
            public static GDIfExpression If(GDExpression condition, GDExpression trueExpr, GDExpression falseExpr) => new GDIfExpression()
            {
                TrueExpression = trueExpr,
                [1] = Syntax.Space(),
                IfKeyword = new GDIfKeyword(),
                [2] = Syntax.Space(),
                Condition = condition,
                [3] = Syntax.Space(),
                ElseKeyword = new GDElseKeyword(),
                [4] = Syntax.Space(),
                FalseExpression = falseExpr
            };

            public static GDArrayInitializerExpression Array() => new GDArrayInitializerExpression();
            public static GDArrayInitializerExpression Array(Func<GDArrayInitializerExpression, GDArrayInitializerExpression> setup) => setup(new GDArrayInitializerExpression());
            public static GDArrayInitializerExpression Array(params GDSyntaxToken[] unsafeTokens) => new GDArrayInitializerExpression() { FormTokensSetter = unsafeTokens };
            public static GDArrayInitializerExpression Array(params GDExpression[] expressions) => new GDArrayInitializerExpression()
            {
                Values = List.Expressions(expressions)
            };

            public static GDDictionaryInitializerExpression Dictionary() => new GDDictionaryInitializerExpression();
            public static GDDictionaryInitializerExpression Dictionary(Func<GDDictionaryInitializerExpression, GDDictionaryInitializerExpression> setup) => setup(new GDDictionaryInitializerExpression());
            public static GDDictionaryInitializerExpression Dictionary(params GDSyntaxToken[] unsafeTokens) => new GDDictionaryInitializerExpression() { FormTokensSetter = unsafeTokens };
            public static GDDictionaryInitializerExpression Dictionary(params GDDictionaryKeyValueDeclaration[] keyValues) => new GDDictionaryInitializerExpression()
            {
                KeyValues = List.KeyValues(keyValues)
            };

            public static GDDictionaryKeyValueDeclaration KeyValue(GDExpression key, GDExpression value) => new GDDictionaryKeyValueDeclaration()
            {
                Key = key,
                Colon = new GDColon(),
                [2] = Syntax.Space(),
                Value = value
            };

            public static GDBoolExpression Bool(bool value) => new GDBoolExpression()
            {
                BoolKeyword = value ? (GDBoolKeyword)new GDTrueKeyword() : new GDFalseKeyword()
            };

            public static GDCallExpression Call() => new GDCallExpression();
            public static GDCallExpression Call(Func<GDCallExpression, GDCallExpression> setup) => setup(new GDCallExpression());
            public static GDCallExpression Call(params GDSyntaxToken[] unsafeTokens) => new GDCallExpression() { FormTokensSetter = unsafeTokens };
            public static GDCallExpression Call(GDExpression caller, params GDExpression[] parameters) => new GDCallExpression()
            {
                CallerExpression = caller,
                OpenBracket = new GDOpenBracket(),
                Parameters = List.Expressions(parameters),
                CloseBracket = new GDCloseBracket()
            };

            public static GDBracketExpression Bracket() => new GDBracketExpression();
            public static GDBracketExpression Bracket(Func<GDBracketExpression, GDBracketExpression> setup) => setup(new GDBracketExpression());
            public static GDBracketExpression Bracket(params GDSyntaxToken[] unsafeTokens) => new GDBracketExpression() { FormTokensSetter = unsafeTokens };
            public static GDBracketExpression Bracket(GDExpression inner) => new GDBracketExpression()
            {
                OpenBracket = new GDOpenBracket(),
                InnerExpression = inner,
                CloseBracket = new GDCloseBracket()
            };

            public static GDMemberOperatorExpression Member() => new GDMemberOperatorExpression();
            public static GDMemberOperatorExpression Member(Func<GDMemberOperatorExpression, GDMemberOperatorExpression> setup) => setup(new GDMemberOperatorExpression());
            public static GDMemberOperatorExpression Member(params GDSyntaxToken[] unsafeTokens) => new GDMemberOperatorExpression() { FormTokensSetter = unsafeTokens };
            public static GDMemberOperatorExpression Member(GDExpression caller, string identifier) => new GDMemberOperatorExpression()
            {
                CallerExpression = caller,
                Point = new GDPoint(),
                Identifier = Syntax.Identifier(identifier)
            };

            public static GDMemberOperatorExpression Member(GDExpression caller, GDIdentifier identifier) => new GDMemberOperatorExpression()
            {
                CallerExpression = caller,
                Point = new GDPoint(),
                Identifier = identifier
            };

            public static GDMemberOperatorExpression BaseMember(string identifier) => new GDMemberOperatorExpression()
            {
                Point = new GDPoint(),
                Identifier = Syntax.Identifier(identifier)
            };

            public static GDMemberOperatorExpression BaseMember(GDIdentifier identifier) => new GDMemberOperatorExpression()
            {
                Point = new GDPoint(),
                Identifier = identifier
            };

            public static GDNumberExpression Number(GDNumber number) => new GDNumberExpression()
            {
                Number = number
            };

            public static GDIndexerExpression Indexer() => new GDIndexerExpression();
            public static GDIndexerExpression Indexer(Func<GDIndexerExpression, GDIndexerExpression> setup) => setup(new GDIndexerExpression());
            public static GDIndexerExpression Indexer(params GDSyntaxToken[] unsafeTokens) => new GDIndexerExpression() { FormTokensSetter = unsafeTokens };
            public static GDIndexerExpression Indexer(GDExpression caller, GDExpression indexExpression) => new GDIndexerExpression()
            {
                CallerExpression = caller,
                SquareOpenBracket = new GDSquareOpenBracket(),
                InnerExpression = indexExpression,
                SquareCloseBracket = new GDSquareCloseBracket()
            };

            public static GDPassExpression Pass() => new GDPassExpression()
            {
                PassKeyword = new GDPassKeyword()
            };

            public static GDBreakPointExpression BreakPoint() => new GDBreakPointExpression()
            {
                BreakPointKeyword = new GDBreakPointKeyword()
            };

            public static GDBreakExpression Break() => new GDBreakExpression()
            {
                BreakKeyword = new GDBreakKeyword()
            };

            public static GDContinueExpression Continue() => new GDContinueExpression()
            {
                ContinueKeyword = new GDContinueKeyword()
            };

            public static GDReturnExpression Return() => new GDReturnExpression()
            {
                ReturnKeyword = new GDReturnKeyword()
            };

            public static GDReturnExpression Return(Func<GDReturnExpression, GDReturnExpression> setup) => setup(new GDReturnExpression());
            public static GDReturnExpression Return(params GDSyntaxToken[] unsafeTokens) => new GDReturnExpression() { FormTokensSetter = unsafeTokens };
            public static GDReturnExpression Return(GDExpression result) => new GDReturnExpression()
            {
                ReturnKeyword = new GDReturnKeyword(),
                [1] = Syntax.Space(),
                Expression = result
            };

            public static GDSingleOperatorExpression SingleOperator() => new GDSingleOperatorExpression();
            public static GDSingleOperatorExpression SingleOperator(Func<GDSingleOperatorExpression, GDSingleOperatorExpression> setup) => setup(new GDSingleOperatorExpression());
            public static GDSingleOperatorExpression SingleOperator(params GDSyntaxToken[] unsafeTokens) => new GDSingleOperatorExpression() { FormTokensSetter = unsafeTokens };
            public static GDSingleOperatorExpression SingleOperator(GDSingleOperator @operator, GDExpression operand) => new GDSingleOperatorExpression()
            {
                Operator = @operator,
                TargetExpression = operand
            };

            public static GDDualOperatorExpression DualOperator() => new GDDualOperatorExpression();
            public static GDDualOperatorExpression DualOperator(Func<GDDualOperatorExpression, GDDualOperatorExpression> setup) => setup(new GDDualOperatorExpression());
            public static GDDualOperatorExpression DualOperator(params GDSyntaxToken[] unsafeTokens) => new GDDualOperatorExpression() { FormTokensSetter = unsafeTokens };
            public static GDDualOperatorExpression DualOperator(GDExpression left, GDDualOperator @operator, GDExpression right) => new GDDualOperatorExpression()
            {
                LeftExpression = left,
                Operator = @operator,
                RightExpression = right
            };

            public static GDGetNodeExpression GetNode() => new GDGetNodeExpression();
            public static GDGetNodeExpression GetNode(Func<GDGetNodeExpression, GDGetNodeExpression> setup) => setup(new GDGetNodeExpression());
            public static GDGetNodeExpression GetNode(params GDSyntaxToken[] unsafeTokens) => new GDGetNodeExpression() { FormTokensSetter = unsafeTokens };
            public static GDGetNodeExpression GetNode(GDPathList pathList) => new GDGetNodeExpression()
            {
                Dollar = new GDDollar(),
                Path = pathList
            };

            public static GDGetNodeExpression GetNode(params GDIdentifier[] names) => new GDGetNodeExpression()
            {
                Dollar = new GDDollar(),
                Path = List.Path(names)
            };

            public static GDBoolExpression True() => new GDBoolExpression()
            {
                BoolKeyword = new GDTrueKeyword()
            };

            public static GDBoolExpression False() => new GDBoolExpression()
            {
                BoolKeyword = new GDFalseKeyword()
            };

            public static GDNodePathExpression NodePath() => new GDNodePathExpression();
            public static GDNodePathExpression NodePath(Func<GDNodePathExpression, GDNodePathExpression> setup) => setup(new GDNodePathExpression());
            public static GDNodePathExpression NodePath(params GDSyntaxToken[] unsafeTokens) => new GDNodePathExpression() { FormTokensSetter = unsafeTokens };
            public static GDNodePathExpression NodePath(GDPathList path) => new GDNodePathExpression()
            {
                Sky = new GDSky(),
                Path = path
            };

            public static GDMatchCaseVariableExpression MatchCaseVariable() => new GDMatchCaseVariableExpression();
            public static GDMatchCaseVariableExpression MatchCaseVariable(Func<GDMatchCaseVariableExpression, GDMatchCaseVariableExpression> setup) => setup(new GDMatchCaseVariableExpression());
            public static GDMatchCaseVariableExpression MatchCaseVariable(params GDSyntaxToken[] unsafeTokens) => new GDMatchCaseVariableExpression() { FormTokensSetter = unsafeTokens };
            public static GDMatchCaseVariableExpression MatchCaseVariable(string identifier) => new GDMatchCaseVariableExpression()
            {
                VarKeyword = new GDVarKeyword(),
                [1] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier)
            };

            public static GDMatchCaseVariableExpression MatchCaseVariable(GDIdentifier identifier) => new GDMatchCaseVariableExpression()
            {
                VarKeyword = new GDVarKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier
            };

            public static GDYieldExpression Yield() => new GDYieldExpression();
            public static GDYieldExpression Yield(Func<GDYieldExpression, GDYieldExpression> setup) => setup(new GDYieldExpression());
            public static GDYieldExpression Yield(params GDSyntaxToken[] unsafeTokens) => new GDYieldExpression() { FormTokensSetter = unsafeTokens };
            public static GDYieldExpression Yield(GDExpressionsList parameters) => new GDYieldExpression()
            {
                YieldKeyword = new GDYieldKeyword(),
                CloseBracket = new GDCloseBracket(),
                Parameters = parameters,
                OpenBracket = new GDOpenBracket()
            };

            public static GDYieldExpression Yield(params GDExpression[] parameters) => new GDYieldExpression()
            {
                YieldKeyword = new GDYieldKeyword(),
                CloseBracket = new GDCloseBracket(),
                Parameters = List.Expressions(parameters),
                OpenBracket = new GDOpenBracket()
            };

            public static GDMatchDefaultOperatorExpression MatchDefaultOperator() => new GDMatchDefaultOperatorExpression()
            {
                DefaultToken = new GDDefaultToken()
            };
        }
    }
}
