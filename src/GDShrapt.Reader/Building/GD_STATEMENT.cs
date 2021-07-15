using System;

namespace GDShrapt.Reader
{
    public static partial class GD
    {
        public static class Statement
        {
            public static GDForStatement For() => new GDForStatement();
            public static GDForStatement For(Func<GDForStatement, GDForStatement> setup) => setup(new GDForStatement());
            public static GDForStatement For(params GDSyntaxToken[] unsafeTokens) => new GDForStatement() { FormTokensSetter = unsafeTokens };
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
                Statements = List.Statements(statements)
            };

            public static GDExpressionStatement Expression(GDExpression expression) => new GDExpressionStatement()
            {
                Expression = expression
            };

            public static GDIfStatement If() => new GDIfStatement();
            public static GDIfStatement If(Func<GDIfStatement, GDIfStatement> setup) => setup(new GDIfStatement());
            public static GDIfStatement If(params GDSyntaxToken[] unsafeTokens) => new GDIfStatement() { FormTokensSetter = unsafeTokens };
            public static GDIfStatement If(GDIfBranch ifBranch) => new GDIfStatement()
            {
                IfBranch = ifBranch
            };

            public static GDIfStatement If(GDIfBranch ifBranch, GDElseBranch elseBranch) => new GDIfStatement()
            {
                IfBranch = ifBranch,
                ElseBranch = elseBranch
            };

            public static GDIfStatement If(GDIfBranch ifBranch, GDElifBranchesList elifBranches) => new GDIfStatement()
            {
                IfBranch = ifBranch,
                ElifBranchesList = elifBranches
            };

            public static GDIfStatement If(GDIfBranch ifBranch, params GDElifBranch[] elifBranches) => new GDIfStatement()
            {
                IfBranch = ifBranch,
                ElifBranchesList = List.ElifBranches(elifBranches)
            };

            public static GDIfStatement If(GDIfBranch ifBranch, GDElifBranchesList elifBranches, GDElseBranch elseBranch) => new GDIfStatement()
            {
                IfBranch = ifBranch,
                ElifBranchesList = elifBranches,
                ElseBranch = elseBranch
            };

            public static GDWhileStatement While() => new GDWhileStatement();
            public static GDWhileStatement While(Func<GDWhileStatement, GDWhileStatement> setup) => setup(new GDWhileStatement());

            public static GDWhileStatement While(params GDSyntaxToken[] unsafeTokens) => new GDWhileStatement() { FormTokensSetter = unsafeTokens };
            public static GDWhileStatement While(GDExpression condition, GDExpression body) => new GDWhileStatement()
            {
                WhileKeyword = new GDWhileKeyword(),
                [1] = Syntax.Space(),
                Condition = condition,
                Colon = Syntax.Colon,
                [3] = Syntax.Space(),
                Expression = body
            };

            public static GDWhileStatement While(GDExpression condition, params GDStatement[] statements) => new GDWhileStatement()
            {
                WhileKeyword = new GDWhileKeyword(),
                [1] = Syntax.Space(),
                Condition = condition,
                Colon = Syntax.Colon,
                Statements = List.Statements(statements)
            };

            public static GDWhileStatement While(GDExpression condition, GDStatementsList statements) => new GDWhileStatement()
            {
                WhileKeyword = new GDWhileKeyword(),
                [1] = Syntax.Space(),
                Condition = condition,
                Colon = Syntax.Colon,
                Statements = statements
            };

            public static GDMatchStatement Match() => new GDMatchStatement();
            public static GDMatchStatement Match(Func<GDMatchStatement, GDMatchStatement> setup) => setup(new GDMatchStatement());
            public static GDMatchStatement Match(params GDSyntaxToken[] unsafeTokens) => new GDMatchStatement() { FormTokensSetter = unsafeTokens };
            public static GDMatchStatement Match(GDExpression value, params GDMatchCaseDeclaration[] cases) => new GDMatchStatement()
            {
                MatchKeyword = new GDMatchKeyword(),
                [1] = Syntax.Space(),
                Value = value,
                Colon = Syntax.Colon,
                Cases = List.MatchCases(cases)
            };
            public static GDMatchStatement Match(GDExpression value, GDMatchCasesList cases) => new GDMatchStatement()
            {
                MatchKeyword = new GDMatchKeyword(),
                [1] = Syntax.Space(),
                Value = value,
                Colon = Syntax.Colon,
                Cases = cases
            };

            public static GDVariableDeclarationStatement Variable() => new GDVariableDeclarationStatement();
            public static GDVariableDeclarationStatement Variable(Func<GDVariableDeclarationStatement, GDVariableDeclarationStatement> setup) => setup(new GDVariableDeclarationStatement());
            public static GDVariableDeclarationStatement Variable(params GDSyntaxToken[] unsafeTokens) => new GDVariableDeclarationStatement() { FormTokensSetter = unsafeTokens };
            public static GDVariableDeclarationStatement Variable(string name) => new GDVariableDeclarationStatement()
            { 
                VarKeyword = new GDVarKeyword(),
                [1] = Syntax.Space(),
                Identifier = name
            };

            public static GDVariableDeclarationStatement Variable(string name, string type) => new GDVariableDeclarationStatement()
            {
                VarKeyword = new GDVarKeyword(),
                [1] = Syntax.Space(),
                Identifier = name,
                [2] = Syntax.Space(),
                Colon = Syntax.Colon,
                [3] = Syntax.Space(),
                Type = type
            };

            public static GDVariableDeclarationStatement Variable(string name, GDExpression initializer) => new GDVariableDeclarationStatement()
            {
                VarKeyword = new GDVarKeyword(),
                [1] = Syntax.Space(),
                Identifier = name,
                [4] = Syntax.Space(),
                Assign = Syntax.Assign,
                [5] = Syntax.Space(),
                Initializer = initializer
            };

            public static GDVariableDeclarationStatement AutoVariable(string name, GDExpression initializer) => new GDVariableDeclarationStatement()
            {
                VarKeyword = new GDVarKeyword(),
                [1] = Syntax.Space(),
                Identifier = name,
                [2] = Syntax.Space(),
                Colon = Syntax.Colon,
                [4] = Syntax.Space(),
                Assign = Syntax.Assign,
                [5] = Syntax.Space(),
                Initializer = initializer
            };

            public static GDVariableDeclarationStatement Variable(string name, string type, GDExpression initializer) => new GDVariableDeclarationStatement()
            {
                VarKeyword = new GDVarKeyword(),
                [1] = Syntax.Space(),
                Identifier = name,
                [2] = Syntax.Space(),
                Colon = Syntax.Colon,
                [3] = Syntax.Space(),
                Type = type,
                [4] = Syntax.Space(),
                Assign = Syntax.Assign,
                [5] = Syntax.Space(),
                Initializer = initializer
            };
        }
    }
}
