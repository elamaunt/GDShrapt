using System.Collections.Generic;

namespace System
{
    public static class SystemExtensionMethods
    {
        public static bool IsNullOrEmpty(this string self) => string.IsNullOrEmpty(self);
        public static bool IsNullOrWhiteSpace(this string self) => string.IsNullOrWhiteSpace(self);

        public static T PushAndPeek<T>(this Stack<T> self, T item)
        {
            self.Push(item);
            return item;
        }
    }
}
