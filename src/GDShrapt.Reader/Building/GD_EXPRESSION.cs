namespace GDShrapt.Reader
{
    public static partial class GD
    {
        public static class Expression
        {
            public static GDIdentifierExpression Identifier(string name) => new GDIdentifierExpression() { Identifier = Syntax.Identifier(name) };
            public static GDIdentifierExpression Identifier(GDIdentifier identifier) => new GDIdentifierExpression() { Identifier = identifier };
            public static GDStringExpression String(string value, bool multiline = false, GDStringBoundingChar boundingChar = GDStringBoundingChar.DoubleQuotas) => new GDStringExpression()
            {
                String = Syntax.String(value, multiline, boundingChar)
            };

            public static GDNumberExpression Number(string value) => new GDNumberExpression() { Number = Syntax.Number(value) };
            public static GDNumberExpression Number(long value) => new GDNumberExpression() { Number = Syntax.Number(value) };
            public static GDNumberExpression Number(double value) => new GDNumberExpression() { Number = Syntax.Number(value) };

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

            public static GDArrayInitializerExpression Array(params GDExpression[] expressions) => new GDArrayInitializerExpression()
            {
                Values = List.Expressions(expressions)
            };

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

            public static GDCallExpression Call(GDExpression caller, params GDExpression[] parameters) => new GDCallExpression()
            {
                CallerExpression = caller,
                OpenBracket = new GDOpenBracket(),
                Parameters = List.Expressions(parameters),
                CloseBracket = new GDCloseBracket()
            };

            public static GDBracketExpression Bracket(GDExpression inner) => new GDBracketExpression()
            {
                OpenBracket = new GDOpenBracket(),
                InnerExpression = inner,
                CloseBracket = new GDCloseBracket()
            };

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

            public static GDIndexerExression Indexer(GDExpression caller, GDExpression indexExpression) => new GDIndexerExression()
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

            public static GDReturnExpression Return(GDExpression result) => new GDReturnExpression()
            {
                ReturnKeyword = new GDReturnKeyword(),
                [1] = Syntax.Space(),
                Expression = result
            };

            public static GDSingleOperatorExpression SingleOperator(GDSingleOperator @operator, GDExpression operand) => new GDSingleOperatorExpression()
            {
                Operator = @operator,
                TargetExpression = operand
            };

            public static GDDualOperatorExpression DualOperator(GDExpression left, GDDualOperator @operator, GDExpression right) => new GDDualOperatorExpression()
            {
                LeftExpression = left,
                Operator = @operator,
                RightExpression = right
            };

            public static GDGetNodeExpression GetNode(GDPathList pathList) => new GDGetNodeExpression()
            {
                Dollar = new GDDollar(),
                Path = pathList
            };

            public static GDBoolExpression True() => new GDBoolExpression()
            {
                BoolKeyword = new GDTrueKeyword()
            };

            public static GDBoolExpression False() => new GDBoolExpression()
            {
                BoolKeyword = new GDFalseKeyword()
            };

            public static GDNodePathExpression NodePath(GDString path) => new GDNodePathExpression()
            {
                At = new GDAt(),
                Path = path
            };

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

            public static GDYieldExpression Yield(GDExpression innerExpression = null) => new GDYieldExpression()
            {
                YieldKeyword = new GDYieldKeyword(),
                CloseBracket = new GDCloseBracket(),
                Expression = innerExpression,
                OpenBracket = new GDOpenBracket()
            };

            public static GDMatchDefaultOperatorExpression MatchDefaultOperator() => new GDMatchDefaultOperatorExpression()
            {
                DefaultToken = new GDDefaultToken()
            };
        }
    }
}
