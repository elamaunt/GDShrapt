using System;
using GDShrapt.Reader;

namespace GDShrapt.Builder
{
    public static partial class GD
    {
        public static class Branch
        {
            public static GDIfBranch If() => new GDIfBranch();
            public static GDIfBranch If(Func<GDIfBranch, GDIfBranch> setup) => setup(new GDIfBranch());
            public static GDIfBranch If(params GDSyntaxToken[] unsafeTokens) => new GDIfBranch() { FormTokensSetter = unsafeTokens };
            public static GDIfBranch If(GDExpression condition, GDExpression expression) => new GDIfBranch()
            {
                IfKeyword = new GDIfKeyword(),
                [1] = Syntax.Space(),
                Condition = condition,
                Colon = new GDColon(),
                [3] = Syntax.Space(),
                Expression = expression
            };

            public static GDIfBranch If(GDExpression condition, params GDStatement[] statements) => new GDIfBranch()
            {
                IfKeyword = new GDIfKeyword(),
                [1] = Syntax.Space(),
                Condition = condition,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDIfBranch If(GDExpression condition, GDStatementsList statements) => new GDIfBranch()
            {
                IfKeyword = new GDIfKeyword(),
                [1] = Syntax.Space(),
                Condition = condition,
                Colon = new GDColon(),
                Statements = statements
            };

            public static GDElifBranch Elif() => new GDElifBranch();
            public static GDElifBranch Elif(Func<GDElifBranch, GDElifBranch> setup) => setup(new GDElifBranch());
            public static GDElifBranch Elif(params GDSyntaxToken[] unsafeTokens) => new GDElifBranch() { FormTokensSetter = unsafeTokens };
            public static GDElifBranch Elif(GDExpression condition, GDExpression expression) => new GDElifBranch()
            {
                ElifKeyword = new GDElifKeyword(),
                [1] = Syntax.Space(),
                Condition = condition,
                Colon = new GDColon(),
                [3] = Syntax.Space(),
                Expression = expression
            };

            public static GDElifBranch Elif(GDExpression condition, params GDStatement[] statements) => new GDElifBranch()
            {
                ElifKeyword = new GDElifKeyword(),
                [1] = Syntax.Space(),
                Condition = condition,
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDElifBranch Elif(GDExpression condition, GDStatementsList statements) => new GDElifBranch()
            {
                ElifKeyword = new GDElifKeyword(),
                [1] = Syntax.Space(),
                Condition = condition,
                Colon = new GDColon(),
                Statements = statements
            };

            public static GDElseBranch Else() => new GDElseBranch();
            public static GDElseBranch Else(Func<GDElseBranch, GDElseBranch> setup) => setup(new GDElseBranch());
            public static GDElseBranch Else(params GDSyntaxToken[] unsafeTokens) => new GDElseBranch() { FormTokensSetter = unsafeTokens };
            public static GDElseBranch Else(GDExpression expression) => new GDElseBranch()
            {
                ElseKeyword = new GDElseKeyword(),
                Colon = new GDColon(),
                [2] = Syntax.Space(),
                Expression = expression
            };

            public static GDElseBranch Else(params GDStatement[] statements) => new GDElseBranch()
            {
                ElseKeyword = new GDElseKeyword(),
                Colon = new GDColon(),
                Statements = List.Statements(statements)
            };

            public static GDElseBranch Else(GDStatementsList statements) => new GDElseBranch()
            {
                ElseKeyword = new GDElseKeyword(),
                Colon = new GDColon(),
                Statements = statements
            };
        }
    }
}
