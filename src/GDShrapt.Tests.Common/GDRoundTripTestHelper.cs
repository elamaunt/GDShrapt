using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Helper for round-trip testing: Parse → Format → Parse → Compare.
    /// </summary>
    public static class GDRoundTripTestHelper
    {
        private static readonly GDScriptReader _reader = new GDScriptReader();

        /// <summary>
        /// Tests that code can be parsed, formatted, and parsed again without changing structure.
        /// Parse → Format → Parse → Compare AST node types.
        /// </summary>
        public static void AssertRoundTrip(string code, GDFormatterOptions options = null)
        {
            // Parse original
            var originalTree = _reader.ParseFileContent(code);
            AssertHelper.NoInvalidTokens(originalTree);

            // Format
            var formatter = new GDFormatter(options ?? GDFormatterOptions.Default);
            var formattedCode = formatter.FormatCode(code);

            // Parse formatted
            var formattedTree = _reader.ParseFileContent(formattedCode);
            AssertHelper.NoInvalidTokens(formattedTree);

            // Compare structure
            CompareAstStructure(originalTree, formattedTree);
        }

        /// <summary>
        /// Tests that formatting is idempotent: Format(Format(code)) == Format(code).
        /// </summary>
        public static void AssertFormatIdempotent(string code, GDFormatterOptions options = null)
        {
            var formatter = new GDFormatter(options ?? GDFormatterOptions.Default);

            // First format
            var firstFormat = formatter.FormatCode(code);

            // Second format
            var secondFormat = formatter.FormatCode(firstFormat);

            // They should be identical
            firstFormat.Should().Be(secondFormat, "formatting should be idempotent");
        }

        /// <summary>
        /// Runs both round-trip and idempotency tests.
        /// </summary>
        public static void AssertFullRoundTrip(string code, GDFormatterOptions options = null)
        {
            AssertRoundTrip(code, options);
            AssertFormatIdempotent(code, options);
        }

        /// <summary>
        /// Loads a sample script from the Scripts folder and runs full round-trip test.
        /// </summary>
        public static void AssertSampleScriptRoundTrip(string scriptName, GDFormatterOptions options = null)
        {
            var scriptPath = Path.Combine(GetScriptsFolder(), scriptName);
            var code = File.ReadAllText(scriptPath);
            AssertFullRoundTrip(code, options);
        }

        /// <summary>
        /// Gets all sample script file paths from the Scripts folder.
        /// </summary>
        public static IEnumerable<string> GetAllSampleScripts()
        {
            var scriptsFolder = GetScriptsFolder();
            return Directory.GetFiles(scriptsFolder, "*.gd")
                .Select(Path.GetFileName)
                .OrderBy(x => x);
        }

        private static string GetScriptsFolder()
        {
            // First try to find Scripts folder in output directory (for linked scripts via csproj)
            var baseDir = System.AppContext.BaseDirectory;
            var outputScripts = Path.Combine(baseDir, "Scripts");
            if (Directory.Exists(outputScripts))
                return outputScripts;

            // Fall back to navigating from bin/Debug/net5.0 to project's Scripts folder
            var projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            return Path.Combine(projectDir, "Scripts");
        }

        private static void CompareAstStructure(GDNode original, GDNode formatted)
        {
            // Compare class members count and types
            if (original is GDClassDeclaration origClass && formatted is GDClassDeclaration formClass)
            {
                // Compare methods
                var origMethods = origClass.Methods.Select(m => m.Identifier?.ToString()).ToList();
                var formMethods = formClass.Methods.Select(m => m.Identifier?.ToString()).ToList();
                origMethods.Should().BeEquivalentTo(formMethods, "method names should match");

                // Compare variables
                var origVars = origClass.Variables.Select(v => v.Identifier?.ToString()).ToList();
                var formVars = formClass.Variables.Select(v => v.Identifier?.ToString()).ToList();
                origVars.Should().BeEquivalentTo(formVars, "variable names should match");

                // Compare signals
                var origSignals = origClass.Signals.Select(s => s.Identifier?.ToString()).ToList();
                var formSignals = formClass.Signals.Select(s => s.Identifier?.ToString()).ToList();
                origSignals.Should().BeEquivalentTo(formSignals, "signal names should match");

                // Compare enums
                var origEnums = origClass.Enums.Select(e => e.Identifier?.ToString()).ToList();
                var formEnums = formClass.Enums.Select(e => e.Identifier?.ToString()).ToList();
                origEnums.Should().BeEquivalentTo(formEnums, "enum names should match");

                // Compare inner classes
                var origInner = origClass.InnerClasses.Select(c => c.Identifier?.ToString()).ToList();
                var formInner = formClass.InnerClasses.Select(c => c.Identifier?.ToString()).ToList();
                origInner.Should().BeEquivalentTo(formInner, "inner class names should match");
            }

            // Compare all nodes count as basic check
            var origNodeCount = original.AllNodes.Count();
            var formNodeCount = formatted.AllNodes.Count();

            // Allow some variance due to whitespace token changes
            formNodeCount.Should().BeInRange(
                (int)(origNodeCount * 0.8),
                (int)(origNodeCount * 1.2),
                "node count should be approximately the same after formatting");
        }
    }
}
