using System;

namespace GDShrapt.Reader
{
    public partial class GD
    {
        public static class Atribute
        {
            public static GDToolAtribute Tool() => new GDToolAtribute()
            {
                ToolKeyword = new GDToolKeyword()
            };

            public static GDClassNameAtribute ClassName() => new GDClassNameAtribute();
            public static GDClassNameAtribute ClassName(Func<GDClassNameAtribute, GDClassNameAtribute> setup) => setup(new GDClassNameAtribute());
            public static GDClassNameAtribute ClassName(params GDSyntaxToken[] unsafeTokens) => new GDClassNameAtribute() { FormTokensSetter = unsafeTokens };
            public static GDClassNameAtribute ClassName(string identifier) => new GDClassNameAtribute()
            {
                ClassNameKeyword = new GDClassNameKeyword(),
                [1] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier)
            };

            public static GDClassNameAtribute ClassName(GDIdentifier identifier) => new GDClassNameAtribute()
            {
                ClassNameKeyword = new GDClassNameKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier
            };

            public static GDExtendsAtribute Extends() => new GDExtendsAtribute();
            public static GDExtendsAtribute Extends(Func<GDExtendsAtribute, GDExtendsAtribute> setup) => setup(new GDExtendsAtribute());
            public static GDExtendsAtribute Extends(params GDSyntaxToken[] unsafeTokens) => new GDExtendsAtribute() { FormTokensSetter = unsafeTokens };
            public static GDExtendsAtribute Extends(GDType type) => new GDExtendsAtribute()
            { 
                ExtendsKeyword = new GDExtendsKeyword(),
                [1] = Syntax.Space(),
                Type = type
            };

            public static GDExtendsAtribute Extends(string type) => new GDExtendsAtribute()
            {
                ExtendsKeyword = new GDExtendsKeyword(),
                [1] = Syntax.Space(),
                Type = Syntax.Type(type)
            };

            public static GDExtendsAtribute Extends(GDString path) => new GDExtendsAtribute()
            {
                ExtendsKeyword = new GDExtendsKeyword(),
                [1] = Syntax.Space(),
                Path = path
            };

            public static GDExtendsAtribute ExtendsPath(string path) => new GDExtendsAtribute()
            {
                ExtendsKeyword = new GDExtendsKeyword(),
                [1] = Syntax.Space(),
                Path = Syntax.String(path)
            };
        }
    }
}
