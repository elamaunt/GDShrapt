using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests
{
    /// <summary>
    /// Extension methods for testing.
    /// </summary>
    public static class TestExtensions
    {
        /// <summary>
        /// Casts object to type T or fails the test.
        /// </summary>
        public static T CastOrAssert<T>(this object self)
        {
            Assert.IsInstanceOfType(self, typeof(T));
            return (T)self;
        }
    }
}
