using System;
using GDShrapt.Reader;

namespace GDShrapt.Builder
{
    public partial class GD
    {
        public static class Attribute
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
                Type = GD.ParseTypeNode(type)
            };

            public static GDExtendsAttribute Extends(GDStringNode path) => new GDExtendsAttribute()
            {
                ExtendsKeyword = new GDExtendsKeyword(),
                [1] = Syntax.Space(),
                Type = new GDStringTypeNode() { Path = path }
            };

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

            public static GDCustomAttribute Custom(string name, params GDExpression[] parameters) => new GDCustomAttribute()
            {
                Attribute = new GDAttribute()
                {
                    At = new GDAt(),
                    Name = Syntax.Identifier(name),
                    OpenBracket = parameters.Length > 0 ? new GDOpenBracket() : null,
                    Parameters = List.Expressions(parameters),
                    CloseBracket = parameters.Length > 0 ? new GDCloseBracket() : null
                }
            };

            public static GDCustomAttribute Export() => Custom("export");
            public static GDCustomAttribute Export(params GDExpression[] parameters) => Custom("export", parameters);

            public static GDCustomAttribute ExportRange(GDExpression min, GDExpression max) => Custom("export_range", min, max);
            public static GDCustomAttribute ExportRange(GDExpression min, GDExpression max, GDExpression step) => Custom("export_range", min, max, step);

            public static GDCustomAttribute ExportEnum(params GDExpression[] values) => Custom("export_enum", values);

            public static GDCustomAttribute ExportFlags(params GDExpression[] flags) => Custom("export_flags", flags);

            public static GDCustomAttribute ExportFile(string filter = null) => filter != null
                ? Custom("export_file", Expression.String(filter))
                : Custom("export_file");

            public static GDCustomAttribute ExportDir() => Custom("export_dir");

            public static GDCustomAttribute ExportMultiline() => Custom("export_multiline");

            public static GDCustomAttribute ExportPlaceholder(string placeholder) => Custom("export_placeholder", Expression.String(placeholder));

            public static GDCustomAttribute ExportColorNoAlpha() => Custom("export_color_no_alpha");

            public static GDCustomAttribute ExportNodePath(params GDExpression[] types) => Custom("export_node_path", types);

            public static GDCustomAttribute ExportGroup(string name, string prefix = null) => prefix != null
                ? Custom("export_group", Expression.String(name), Expression.String(prefix))
                : Custom("export_group", Expression.String(name));

            public static GDCustomAttribute ExportSubgroup(string name, string prefix = null) => prefix != null
                ? Custom("export_subgroup", Expression.String(name), Expression.String(prefix))
                : Custom("export_subgroup", Expression.String(name));

            public static GDCustomAttribute ExportCategory(string name) => Custom("export_category", Expression.String(name));

            public static GDCustomAttribute Onready() => Custom("onready");

            public static GDCustomAttribute Icon(string path) => Custom("icon", Expression.String(path));

            public static GDCustomAttribute WarningIgnore(params string[] warnings)
            {
                var expressions = new GDExpression[warnings.Length];
                for (int i = 0; i < warnings.Length; i++)
                    expressions[i] = Expression.String(warnings[i]);
                return Custom("warning_ignore", expressions);
            }

            public static GDCustomAttribute Rpc(params GDExpression[] parameters) => Custom("rpc", parameters);

            public static GDCustomAttribute StaticUnload() => Custom("static_unload");

            public static GDCustomAttribute Abstract() => Custom("abstract");

            public static GDCustomAttribute PublicApi() => Custom("public_api");
            public static GDCustomAttribute DynamicUse() => Custom("dynamic_use");
        }
    }
}
