using System.Collections.Generic;

namespace System
{
    internal static class SystemExtensionMethods
    {
        public static bool IsNullOrEmpty(this string self) => string.IsNullOrEmpty(self);
        public static bool IsNullOrWhiteSpace(this string self) => string.IsNullOrWhiteSpace(self);

        public static T PushAndPeek<T>(this Stack<T> self, T item)
        {
            self.Push(item);
            return item;
        }
        public static T PeekOrDefault<T>(this Stack<T> self)
        {
            if (self.Count == 0)
                return default;

            return self.Peek();
        }
    }
}
