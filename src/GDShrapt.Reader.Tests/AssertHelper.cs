using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader.Tests
{
    public static class AssertHelper
    {
        internal static void CompareCodeStrings(string s1, string s2)
        {
            s1 = s1.Replace("\r\n", "\n").Replace("    ", "\t");
            s2 = s2.Replace("\r\n", "\n").Replace("    ", "\t");

#if DEBUG
            bool diffFound = false;
            var original = new StringBuilder();
            var other = new StringBuilder();

            if (s1.Length == s2.Length)
            {
                for (int i = 0; i < s1.Length; i++)
                {
                    var ch1 = s1[i];
                    var ch2 = s2[i];

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

            Assert.AreEqual(s1, s2, "The code strings are not same");
        }

        internal static void NoInvalidTokens(GDNode node)
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
