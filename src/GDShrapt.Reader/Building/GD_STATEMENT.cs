namespace GDShrapt.Reader
{
    public static partial class GD
    {
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
                Statements = List.Statements(statements)
            };

            public static GDExpressionStatement Expression(GDExpression expression) => new GDExpressionStatement()
            {
                Expression = expression
            };

            public static GDIfStatement If(GDIfBranch ifBranch) => new GDIfStatement()
            {
                IfBranch = ifBranch
            };

            public static GDIfStatement If(GDIfBranch ifBranch, GDElseBranch elseBranch) => new GDIfStatement()
            {
                IfBranch = ifBranch,
                ElseBranch = elseBranch
            };

            public static GDIfStatement If(GDIfBranch ifBranch, GDElifBranchesList elifBranches, GDElseBranch elseBranch) => new GDIfStatement()
            {
                IfBranch = ifBranch,
                ElifBranchesList = elifBranches,
                ElseBranch = elseBranch
            };
        }
    }
}
