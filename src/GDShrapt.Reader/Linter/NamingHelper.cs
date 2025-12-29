using System.Text.RegularExpressions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Helper class for checking naming conventions.
    /// </summary>
    public static class NamingHelper
    {
        // Regex patterns for naming conventions
        private static readonly Regex SnakeCasePattern = new Regex(@"^[a-z][a-z0-9]*(_[a-z0-9]+)*$", RegexOptions.Compiled);
        private static readonly Regex PascalCasePattern = new Regex(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);
        private static readonly Regex CamelCasePattern = new Regex(@"^[a-z][a-zA-Z0-9]*$", RegexOptions.Compiled);
        private static readonly Regex ScreamingSnakeCasePattern = new Regex(@"^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$", RegexOptions.Compiled);

        // Private prefix pattern (underscore at start)
        private static readonly Regex PrivatePrefixPattern = new Regex(@"^_", RegexOptions.Compiled);

        /// <summary>
        /// Checks if a name matches the expected case convention.
        /// </summary>
        public static bool MatchesCase(string name, NamingCase expectedCase)
        {
            if (string.IsNullOrEmpty(name))
                return true;

            // Strip leading underscore for checking (private prefix)
            var nameToCheck = name.StartsWith("_") ? name.Substring(1) : name;
            if (string.IsNullOrEmpty(nameToCheck))
                return true;

            switch (expectedCase)
            {
                case NamingCase.SnakeCase:
                    return SnakeCasePattern.IsMatch(nameToCheck);

                case NamingCase.PascalCase:
                    return PascalCasePattern.IsMatch(nameToCheck);

                case NamingCase.CamelCase:
                    return CamelCasePattern.IsMatch(nameToCheck);

                case NamingCase.ScreamingSnakeCase:
                    return ScreamingSnakeCasePattern.IsMatch(nameToCheck);

                case NamingCase.Any:
                    return true;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Checks if a name has a private prefix (starts with underscore).
        /// </summary>
        public static bool HasPrivatePrefix(string name)
        {
            return !string.IsNullOrEmpty(name) && PrivatePrefixPattern.IsMatch(name);
        }

        /// <summary>
        /// Gets the expected case name for display in messages.
        /// </summary>
        public static string GetCaseName(NamingCase namingCase)
        {
            switch (namingCase)
            {
                case NamingCase.SnakeCase:
                    return "snake_case";
                case NamingCase.PascalCase:
                    return "PascalCase";
                case NamingCase.CamelCase:
                    return "camelCase";
                case NamingCase.ScreamingSnakeCase:
                    return "SCREAMING_SNAKE_CASE";
                case NamingCase.Any:
                    return "any case";
                default:
                    return namingCase.ToString();
            }
        }

        /// <summary>
        /// Suggests a corrected name based on the expected case.
        /// </summary>
        public static string SuggestCorrectName(string name, NamingCase expectedCase)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var hasUnderscore = name.StartsWith("_");
            var baseName = hasUnderscore ? name.Substring(1) : name;

            string result;
            switch (expectedCase)
            {
                case NamingCase.SnakeCase:
                    result = ToSnakeCase(baseName);
                    break;

                case NamingCase.PascalCase:
                    result = ToPascalCase(baseName);
                    break;

                case NamingCase.CamelCase:
                    result = ToCamelCase(baseName);
                    break;

                case NamingCase.ScreamingSnakeCase:
                    result = ToScreamingSnakeCase(baseName);
                    break;

                default:
                    result = baseName;
                    break;
            }

            return hasUnderscore ? "_" + result : result;
        }

        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Handle already snake_case
            if (SnakeCasePattern.IsMatch(name))
                return name;

            // Convert PascalCase/camelCase to snake_case
            var result = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2");
            result = Regex.Replace(result, "([A-Z]+)([A-Z][a-z])", "$1_$2");
            return result.ToLowerInvariant();
        }

        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Handle snake_case
            if (name.Contains("_"))
            {
                var parts = name.Split('_');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Length > 0)
                    {
                        parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
                    }
                }
                return string.Join("", parts);
            }

            // Already PascalCase or camelCase - just ensure first letter is uppercase
            if (name.Length > 0)
            {
                return char.ToUpperInvariant(name[0]) + name.Substring(1);
            }

            return name;
        }

        private static string ToCamelCase(string name)
        {
            var pascal = ToPascalCase(name);
            if (pascal.Length > 0)
            {
                return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
            }
            return pascal;
        }

        private static string ToScreamingSnakeCase(string name)
        {
            return ToSnakeCase(name).ToUpperInvariant();
        }
    }
}
