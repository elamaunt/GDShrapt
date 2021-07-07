namespace GDShrapt.Reader
{
    public static partial class GD
    {
        public static class Declaration
        {
            public static GDClassDeclaration Class(GDClassAtributesList atributes, GDClassMembersList members) => new GDClassDeclaration()
            {
                Atributes = atributes,
                [1] = new GDNewLine(),
                Members = members
            };

            public static GDInnerClassDeclaration InnerClass(string name, GDClassMembersList members) => new GDInnerClassDeclaration()
            {
                ClassKeyword = new GDClassKeyword(),
                [1] = Syntax.Space(),
                Identifier = Syntax.Identifier(name),
                Colon = new GDColon(),
                [3] = new GDNewLine(),
                Members = members
            };

            public static GDInnerClassDeclaration InnerClass(GDIdentifier identifier, GDClassMembersList members) => new GDInnerClassDeclaration()
            {
                ClassKeyword = new GDClassKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier,
                Colon = new GDColon(),
                [3] = new GDNewLine(),
                Members = members
            };

            public static GDClassDeclaration Class(GDClassAtributesList atributes, params GDClassMember[] members) => new GDClassDeclaration()
            {
                Atributes = atributes,
                [1] = new GDNewLine(),
                Members = List.Members(members)
            };

            public static GDInnerClassDeclaration InnerClass(string name, params GDClassMember[] members) => new GDInnerClassDeclaration()
            {
                ClassKeyword = new GDClassKeyword(),
                [1] = Syntax.Space(),
                Identifier = Syntax.Identifier(name),
                Colon = new GDColon(),
                [3] = new GDNewLine(),
                Members = List.Members(members)
            };

            public static GDInnerClassDeclaration InnerClass(GDIdentifier identifier, params GDClassMember[] members) => new GDInnerClassDeclaration()
            {
                ClassKeyword = new GDClassKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier,
                Colon = new GDColon(),
                [3] = new GDNewLine(),
                Members = List.Members(members)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, GDParametersList parameters, bool isStatic = false, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                Colon = new GDColon(),
                OpenBracket = new GDOpenBracket(),
                Parameters = parameters,
                CloseBracket = new GDCloseBracket(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, params GDStatement[] statements) => new GDMethodDeclaration( )
            { 
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, GDType returnType, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                [10] = Syntax.Space(),
                ReturnTypeKeyword = new GDReturnTypeKeyword(),
                [11] = Syntax.Space(),
                ReturnType = returnType,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration Method(GDIdentifier identifier, GDType returnType, GDExpression[] baseCallParameters, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                BaseCallPoint = new GDPoint(),
                BaseCallOpenBracket = new GDOpenBracket(),
                BaseCallParameters = List.Expressions(baseCallParameters),
                BaseCallCloseBracket =new GDCloseBracket(),
                [10] = Syntax.Space(),
                ReturnTypeKeyword = new GDReturnTypeKeyword(),
                [11] = Syntax.Space(),
                ReturnType = returnType,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration StaticMethod(GDIdentifier identifier, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                StaticKeyword = new GDStaticKeyword(),
                [1] = Syntax.Space(),
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration StaticMethod(GDIdentifier identifier, GDType returnType, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                StaticKeyword = new GDStaticKeyword(),
                [1] = Syntax.Space(),
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                [10] = Syntax.Space(),
                ReturnTypeKeyword = new GDReturnTypeKeyword(),
                [11] = Syntax.Space(),
                ReturnType = returnType,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDMethodDeclaration StaticMethod(GDIdentifier identifier, GDType returnType, GDExpression[] baseCallParameters, params GDStatement[] statements) => new GDMethodDeclaration()
            {
                StaticKeyword = new GDStaticKeyword(),
                [1] = Syntax.Space(),
                FuncKeyword = new GDFuncKeyword(),
                [2] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                CloseBracket = new GDCloseBracket(),
                BaseCallPoint = new GDPoint(),
                BaseCallOpenBracket = new GDOpenBracket(),
                BaseCallParameters = List.Expressions(baseCallParameters),
                BaseCallCloseBracket = new GDCloseBracket(),
                [10] = Syntax.Space(),
                ReturnTypeKeyword = new GDReturnTypeKeyword(),
                [11] = Syntax.Space(),
                ReturnType = returnType,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDParameterDeclaration Parameter(string identifier) => new GDParameterDeclaration()
            {
                Identifier = Syntax.Identifier(identifier)
            };

            public static GDParameterDeclaration Parameter(string identifier, string type, GDExpression defaultValue) => new GDParameterDeclaration()
            {
                Identifier = Syntax.Identifier(identifier),
                [1] = Syntax.Space(),
                Colon = new GDColon(),
                [2] = Syntax.Space(),
                Type = Syntax.Type(type),
                [3] = Syntax.Space(),
                Assign = new GDAssign(),
                [4] = Syntax.Space(),
                DefaultValue = defaultValue
            };

            public static GDParameterDeclaration Parameter(string identifier, GDExpression defaultValue) => new GDParameterDeclaration()
            {
                Identifier = Syntax.Identifier(identifier),
                [3] = Syntax.Space(),
                Assign = new GDAssign(),
                [4] = Syntax.Space(),
                DefaultValue = defaultValue
            };

            public static GDParameterDeclaration Parameter(GDIdentifier identifier) => new GDParameterDeclaration()
            { 
                Identifier = identifier
            };

            public static GDParameterDeclaration Parameter(GDIdentifier identifier, GDType type, GDExpression defaultValue) => new GDParameterDeclaration()
            {
                Identifier = identifier,
                [1] = Syntax.Space(),
                Colon = new GDColon(),
                [2] = Syntax.Space(),
                Type = type,
                [3] = Syntax.Space(),
                Assign = new GDAssign(),
                [4] = Syntax.Space(),
                DefaultValue = defaultValue
            };

            public static GDParameterDeclaration Parameter(GDIdentifier identifier, GDExpression defaultValue) => new GDParameterDeclaration()
            {
                Identifier = identifier,
                [3] = Syntax.Space(),
                Assign = new GDAssign(),
                [4] = Syntax.Space(),
                DefaultValue = defaultValue
            };

            public static GDMatchCaseDeclaration MatchCase(GDExpressionsList conditions, GDStatementsList statements) => new GDMatchCaseDeclaration()
            { 
                Conditions = conditions,
                Colon = new GDColon(),
                Statements = statements
            };

            public static GDMatchCaseDeclaration MatchCase(GDExpressionsList conditions, params GDStatement[] statements) => new GDMatchCaseDeclaration()
            {
                Conditions = conditions,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDSignalDeclaration Signal(GDIdentifier identifier, GDParametersList parameters) => new GDSignalDeclaration()
            {
                SignalKeyword = new GDSignalKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                Parameters = parameters,
                CloseBracket = new GDCloseBracket()
            };

            public static GDSignalDeclaration Signal(string identifier, params GDParameterDeclaration[] parameters) => new GDSignalDeclaration()
            {
                SignalKeyword = new GDSignalKeyword(),
                [1] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                OpenBracket = new GDOpenBracket(),
                Parameters = List.Parameters(parameters),
                CloseBracket = new GDCloseBracket()
            };

            public static GDSignalDeclaration Signal(GDIdentifier identifier, params GDParameterDeclaration[] parameters) => new GDSignalDeclaration()
            {
                SignalKeyword = new GDSignalKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier,
                OpenBracket = new GDOpenBracket(),
                Parameters = List.Parameters(parameters),
                CloseBracket = new GDCloseBracket()
            };

            public static GDVariableDeclaration Variable(GDIdentifier identifier) => new GDVariableDeclaration()
            { 
                VarKeyword = new GDVarKeyword(),
                [4] = Syntax.Space(),
                Identifier = identifier
            };

            public static GDVariableDeclaration Variable(string identifier) => new GDVariableDeclaration()
            {
                VarKeyword = new GDVarKeyword(),
                [4] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier)
            };

            public static GDVariableDeclaration Variable(string identifier, string type) => new GDVariableDeclaration()
            {
                VarKeyword = new GDVarKeyword(),
                [4] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [5] = Syntax.Space(),
                Colon = new GDColon(),
                [6] = Syntax.Space(),
                Type = Syntax.Type(type)
            };

            public static GDVariableDeclaration Variable(string identifier, string type, GDExpression initializer) => new GDVariableDeclaration()
            {
                VarKeyword = new GDVarKeyword(),
                [4] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [5] = Syntax.Space(),
                Colon = new GDColon(),
                [6] = Syntax.Space(),
                Type = Syntax.Type(type),
                [7] = Syntax.Space(),
                Assign = new GDAssign(),
                [8] = Syntax.Space(),
                Initializer = initializer
            };

            public static GDVariableDeclaration Variable(string identifier, string type, GDExportDeclaration export, GDExpression initializer) => new GDVariableDeclaration()
            {
                Export = export,
                [3] = Syntax.Space(),
                VarKeyword = new GDVarKeyword(),
                [4] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [5] = Syntax.Space(),
                Colon = new GDColon(),
                [6] = Syntax.Space(),
                Type = Syntax.Type(type),
                [7] = Syntax.Space(),
                Assign = new GDAssign(),
                [8] = Syntax.Space(),
                Initializer = initializer
            };

            public static GDVariableDeclaration Variable(string identifier, string type, GDExpression initializer, GDIdentifier setMethod, GDIdentifier getMethod) => new GDVariableDeclaration()
            {
                VarKeyword = new GDVarKeyword(),
                [4] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier),
                [5] = Syntax.Space(),
                Colon = new GDColon(),
                [6] = Syntax.Space(),
                Type = Syntax.Type(type),
                [7] = Syntax.Space(),
                Assign = new GDAssign(),
                [8] = Syntax.Space(),
                Initializer = initializer,
                [9] = Syntax.Space(),
                SetGetKeyword = new GDSetGetKeyword(),
                SetMethodIdentifier = setMethod,
                [11] = Syntax.Space(),
                Comma = new GDComma(),
                GetMethodIdentifier = getMethod
            };
        }
    }
}
