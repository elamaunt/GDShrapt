using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Reader.Tests
{
    public static class TestingExtensionMethods
    {

        public static T CastOrAssert<T>(this object self)
        {
            Assert.IsInstanceOfType(self, typeof(T));
            return (T)self;
        }
    }
}
