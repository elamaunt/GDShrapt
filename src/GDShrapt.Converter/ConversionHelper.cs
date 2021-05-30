using GDShrapt.Reader;
using Microsoft.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace GDShrapt.Converter
{
    public static class ConversionHelper
    {
        public static string ConvertDeclarationNameToSharpNamingStyle(string name)
        {
            if (name == null)
                return null;

            var builder = new StringBuilder();

            var needTitleCase = true;

            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '_')
                {
                    needTitleCase = true;
                    continue;
                }

                if (needTitleCase)
                    builder.Append(CultureInfo.InvariantCulture.TextInfo.ToUpper(name[i]));
                else
                    builder.Append(name[i]);
            }

            return builder.ToString();
        }

        public static string ConvertVariableNameToSharpNamingStyle(string name)
        {
            if (name == null)
                return null;

            var builder = new StringBuilder();

            var needTitleCase = false;

            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '_')
                {
                    needTitleCase = true;
                    continue;
                }

                if (needTitleCase)
                    builder.Append(CultureInfo.InvariantCulture.TextInfo.ToUpper(name[i]));
                else
                    builder.Append(name[i]);
            }

            return builder.ToString();
        }

        public static string ExtractType(ConversionSettings settings, GDVariableDeclaration d)
        {
            var typeName = d.Type.Sequence;

            if (string.IsNullOrWhiteSpace(typeName))
            {
                if (d.Type.ExtractTypeFromInitializer)
                {
                    typeName = GetTypeFromExpression(d.Initializer);

                    if (settings.ConvertGDScriptNamingStyleToSharp)
                        typeName = ConvertDeclarationNameToSharpNamingStyle(typeName);

                    return typeName;
                }
                else
                {
                    return "dynamic";
                }

            }
            else
            {
                if (settings.ConvertGDScriptNamingStyleToSharp)
                    typeName = ConvertDeclarationNameToSharpNamingStyle(typeName);

                return typeName;
            }
        }

        private static string GetTypeFromExpression(GDExpression e)
        {
            return "dynamic";
           // var 
           // e.
        }

        public static string GetName(ConversionSettings settings, GDIdentifier identifier)
        {
            var name = identifier.Sequence;

            if (settings.ConvertGDScriptNamingStyleToSharp)
                name = ConvertVariableNameToSharpNamingStyle(name);

            return name;
        }
    }
}