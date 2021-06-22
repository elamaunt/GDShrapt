using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
