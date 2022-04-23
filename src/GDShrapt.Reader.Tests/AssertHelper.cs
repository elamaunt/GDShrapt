using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;

namespace GDShrapt.Reader.Tests
{
    public static class AssertHelper
    {
        internal static void CompareCodeStrings(string s1, string s2)
        {
            Assert.AreEqual(
                s1.Replace("\r", "").Replace("    ", "\t"), 
                s2.Replace("\r", "").Replace("    ", "\t"));
        }
        internal static void NoInvalidTokens(GDNode node)
        {
            var invalidTokens = node.AllInvalidTokens.ToArray();
            var messageBuilder = new StringBuilder();

            for (int i = 0; i < invalidTokens.Length; i++)
                messageBuilder.AppendLine(i + "." + invalidTokens[i]);

            Assert.AreEqual(0, invalidTokens.Length, messageBuilder.ToString());
        }
    }
}
