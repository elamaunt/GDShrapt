using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Common assertion helpers for GDShrapt tests.
    /// </summary>
    public static class AssertHelper
    {
        /// <summary>
        /// Compares code strings, normalizing line endings and indentation.
        /// </summary>
        public static void CompareCodeStrings(string expected, string actual)
        {
            expected = expected.Replace("\r\n", "\n").Replace("    ", "\t");
            actual = actual.Replace("\r\n", "\n").Replace("    ", "\t");

#if DEBUG
            bool diffFound = false;
            var original = new StringBuilder();
            var other = new StringBuilder();

            if (expected.Length == actual.Length)
            {
                for (int i = 0; i < expected.Length; i++)
                {
                    var ch1 = expected[i];
                    var ch2 = actual[i];

                    if (ch1 != ch2)
                        diffFound = true;

                    if (diffFound)
                    {
                        original.Append(ch1);
                        other.Append(ch2);
                    }
                }
            }
#endif

            Assert.AreEqual(expected, actual, "The code strings are not same");
        }

        /// <summary>
        /// Asserts that the node has no invalid tokens.
        /// </summary>
        public static void NoInvalidTokens(GDNode node)
        {
            var invalidTokens = node.AllInvalidTokens.ToArray();
            var messageBuilder = new StringBuilder();

            messageBuilder.AppendLine();
            for (int i = 0; i < invalidTokens.Length; i++)
            {
                var token = invalidTokens[i];
                messageBuilder.AppendLine($"{token.StartLine}.{token.StartColumn}: " + token);
            }

            Assert.AreEqual(0, invalidTokens.Length, messageBuilder.ToString(), "There are invalid tokens in the code");
        }
    }
}
