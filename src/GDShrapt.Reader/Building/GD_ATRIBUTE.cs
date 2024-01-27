using System;

namespace GDShrapt.Reader
{
    public partial class GD
    {
        public static class Atribute
        {
            public static GDToolAttribute Tool() => new GDToolAttribute()
            {
                ToolKeyword = new GDToolKeyword()
            };

            public static GDClassNameAttribute ClassName() => new GDClassNameAttribute();
            public static GDClassNameAttribute ClassName(Func<GDClassNameAttribute, GDClassNameAttribute> setup) => setup(new GDClassNameAttribute());
            public static GDClassNameAttribute ClassName(params GDSyntaxToken[] unsafeTokens) => new GDClassNameAttribute() { FormTokensSetter = unsafeTokens };
            public static GDClassNameAttribute ClassName(string identifier) => new GDClassNameAttribute()
            {
                ClassNameKeyword = new GDClassNameKeyword(),
                [1] = Syntax.Space(),
                Identifier = Syntax.Identifier(identifier)
            };

            public static GDClassNameAttribute ClassName(GDIdentifier identifier) => new GDClassNameAttribute()
            {
                ClassNameKeyword = new GDClassNameKeyword(),
                [1] = Syntax.Space(),
                Identifier = identifier
            };

            public static GDExtendsAttribute Extends() => new GDExtendsAttribute();
            public static GDExtendsAttribute Extends(Func<GDExtendsAttribute, GDExtendsAttribute> setup) => setup(new GDExtendsAttribute());
            public static GDExtendsAttribute Extends(params GDSyntaxToken[] unsafeTokens) => new GDExtendsAttribute() { FormTokensSetter = unsafeTokens };
            public static GDExtendsAttribute Extends(GDTypeNode type) => new GDExtendsAttribute()
            { 
                ExtendsKeyword = new GDExtendsKeyword(),
                [1] = Syntax.Space(),
                Type = type
            };

            public static GDExtendsAttribute Extends(string type) => new GDExtendsAttribute()
            {
                ExtendsKeyword = new GDExtendsKeyword(),
                [1] = Syntax.Space(),
                Type = GDResolvingHelper.ParseTypeNode(type)
            };

            public static GDExtendsAttribute Extends(GDStringNode path) => new GDExtendsAttribute()
            {
                ExtendsKeyword = new GDExtendsKeyword(),
                [1] = Syntax.Space(),
                Type = new GDStringTypeNode() { Path = path }
            };

            /*public static GDExtendsAttribute ExtendsPath(string path) => new GDExtendsAttribute()
            {
                ExtendsKeyword = new GDExtendsKeyword(),
                [1] = Syntax.Space(),
                Type = new GDStringTypeNode() { Path = path }
            };*/

            public static GDCustomAttribute Custom() => new GDCustomAttribute();
            public static GDCustomAttribute Custom(Func<GDCustomAttribute, GDCustomAttribute> setup) => setup(new GDCustomAttribute());
            public static GDCustomAttribute Custom(params GDSyntaxToken[] unsafeTokens) => new GDCustomAttribute() { FormTokensSetter = unsafeTokens };
           
            public static GDCustomAttribute Custom(GDAttribute attribute) => new GDCustomAttribute()
            { 
                Attribute = attribute
            };

            public static GDCustomAttribute Custom(GDIdentifier identifier) => new GDCustomAttribute()
            {
                Attribute = new GDAttribute() 
                { 
                    At = new GDAt(),
                    Name = identifier
                }
            };
        }
    }
}
