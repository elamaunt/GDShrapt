namespace GDShrapt.Reader
{
    public static partial class GD
    {
        public static class Syntax
        {
            public static GDIdentifier Identifier(string name) => new GDIdentifier() { Sequence = name };
            public static GDType Type(string name) => new GDType() { Sequence = name };
            public static GDString String(string value, bool multiline = false, GDStringBoundingChar boundingChar = GDStringBoundingChar.DoubleQuotas) => new GDString()
            {
                Value = value,
                Multiline = multiline,
                BoundingChar = boundingChar
            };

            public static GDNumber Number(string stringValue) => new GDNumber() { ValueAsString = stringValue };
            public static GDNumber Number(long value) => new GDNumber() { ValueInt64 = value };
            public static GDNumber Number(double value) => new GDNumber() { ValueDouble = value };

            public static GDSpace Space(int count = 1) => new GDSpace() { Sequence = new string(' ', count) };
            public static GDNewLine NewLine() => new GDNewLine();
            public static GDIntendation Intendation(int count = 0) => new GDIntendation() { Sequence = new string('\t', count), LineIntendationThreshold = count };
            public static GDComment Comment(string comment) => new GDComment() { Sequence = comment };
        }

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
        }

        public static class Statement
        {
            public static GDForStatement For(GDIdentifier variable, GDExpression collection, GDExpression body) => new GDForStatement() 
            {
                ForKeyword = new GDForKeyword(),
                [1] = Syntax.Space(),
                Variable = variable,
                [2] = Syntax.Space(),
                InKeyword = new GDInKeyword(),
                [3] = Syntax.Space(),
                Collection = collection,
                Colon = new GDColon(),
                [5] = Syntax.Space(),
                Expression = body
            };

            public static GDForStatement For(GDIdentifier variable, GDExpression collection, params GDStatement[] statements) => new GDForStatement()
            {
                ForKeyword = new GDForKeyword(),
                [1] = Syntax.Space(),
                Variable = variable,
                [2] = Syntax.Space(),
                InKeyword = new GDInKeyword(),
                [3] = Syntax.Space(),
                Collection = collection,
                [4] = Syntax.Space(),
                Colon = new GDColon(),
                [5] = Syntax.NewLine(),
                Statements = List.Statements(statements)
            };

            public static GDExpressionStatement Expression(GDExpression expression) => new GDExpressionStatement()
            {
                Expression = expression
            };
        }

        public static class List
        {
            public static GDStatementsList Statements(params GDStatement[] statements)
            {
                if (statements == null || statements.Length == 0)
                    return null;

                var list = new GDStatementsList();

                for (int i = 0; i < statements.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.Add(new GDNewLine());
                        list.Form.Add(Syntax.Intendation());
                    }

                    list.Add(statements[i]);
                }

                return list;
            }

            public static GDExpressionsList Expressions(params GDExpression[] expressions)
            {
                if (expressions == null || expressions.Length == 0)
                    return null;

                var list = new GDExpressionsList();

                for (int i = 0; i < expressions.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.Add(new GDComma());
                        list.Form.Add(Syntax.Space());
                    }

                    list.Add(expressions[i]);
                }

                return list;
            }

            public static GDDictionaryKeyValueDeclarationList KeyValues(params GDDictionaryKeyValueDeclaration[] keyValues)
            {
                var list = new GDDictionaryKeyValueDeclarationList();

                for (int i = 0; i < keyValues.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.Add(new GDComma());
                        list.Form.Add(Syntax.Space());
                    }

                    list.Add(keyValues[i]);
                }

                return list;
            }

            public static GDParametersList Parameters(params GDParameterDeclaration[] parameters)
            {
                var list = new GDParametersList();

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        list.Form.Add(new GDComma());
                        list.Form.Add(Syntax.Space());
                    }

                    list.Add(parameters[i]);
                }

                return list;
            }
        }

        public static class Declaration
        {
            public static GDMethodDeclaration Method(GDIdentifier identifier, GDParametersList parameters, bool isStatic = false, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                Colon = new GDColon(),
                OpenBracket = new GDOpenBracket(),
                Parameters = parameters,
                CloseBracket = new GDCloseBracket(),
                [13] = new GDNewLine(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, bool isStatic = false, params GDStatement[] statements) => new GDMethodDeclaration( )
            { 
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                Colon = new GDColon(),
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                [13] = new GDNewLine(),
                Statements = List.Statements(statements)
            };

            public static GDParameterDeclaration Parameter(string identifier) => new GDParameterDeclaration()
            {
                Identifier = Syntax.Identifier(identifier)
            };

            public static GDParameterDeclaration Parameter(GDIdentifier identifier) => new GDParameterDeclaration()
            { 
                Identifier = identifier
            };
        }
    }
}
