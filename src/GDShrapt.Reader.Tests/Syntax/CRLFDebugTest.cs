using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GDShrapt.Reader.Tests
{
    [TestClass]
    public class CRLFDebugTest
    {
        [TestMethod]
        public void Debug_SimplerCase()
        {
            var reader = new GDScriptReader();
            // Simpler case - just one parameter with array default
            var code = "func f(p: Array = []\r\n):\r\n\tpass\r\n";

            var tree = reader.ParseFileContent(code);

            // Check for any CR tokens
            var crTokens = tree.AllTokens.OfType<GDCarriageReturnToken>().ToList();
            System.Console.WriteLine($"Found {crTokens.Count} CR tokens");
            foreach (var cr in crTokens)
            {
                System.Console.WriteLine($"  CR at {cr.StartLine}:{cr.StartColumn}, parent: {cr.Parent?.GetType().Name}");
            }

            // Check NL tokens too
            var nlTokens = tree.AllTokens.OfType<GDNewLine>().ToList();
            System.Console.WriteLine($"Found {nlTokens.Count} NL tokens");
            foreach (var nl in nlTokens)
            {
                System.Console.WriteLine($"  NL at {nl.StartLine}:{nl.StartColumn}, parent: {nl.Parent?.GetType().Name}");
            }

            // Walk the method's parameters
            var method = tree.Members.OfType<GDMethodDeclaration>().FirstOrDefault();
            if (method != null)
            {
                System.Console.WriteLine($"\nMethod parameters list tokens:");
                foreach (var token in method.Parameters.AllTokens)
                {
                    System.Console.WriteLine($"  {token.GetType().Name}: '{Escape(token.ToString())}'");
                }

                System.Console.WriteLine($"\nFirst parameter tokens:");
                var param = method.Parameters.FirstOrDefault();
                if (param != null)
                {
                    foreach (var token in param.AllTokens)
                    {
                        System.Console.WriteLine($"  {token.GetType().Name}: '{Escape(token.ToString())}'");
                    }
                }
            }

            System.Console.WriteLine($"\nOriginal: {Escape(code)}");
            System.Console.WriteLine($"Output:   {Escape(tree.ToOriginalString())}");

            tree.ToOriginalString().Should().Be(code);
        }

        static string Escape(string s) => s?.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t") ?? "null";
    }
}
