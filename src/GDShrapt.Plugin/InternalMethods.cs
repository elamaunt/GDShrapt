using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal static class InternalMethods
{
    public static B GetOrDefault<T,B> (this IDictionary<T, B> self, T key, B defaultValue = default)
    {
        if (self.TryGetValue(key, out B value))
            return value;
        return defaultValue;
    }

    public static B GetOrDefault<T, B>(this ConditionalWeakTable<T, B> self, T key, B defaultValue = default)
        where T : class
        where B : class
    {
        if (self.TryGetValue(key, out B value))
            return value;
        return defaultValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrintIfFailed(this Task task)
    {
        task.ContinueWith(t =>
        {
            Logger.Info($"Async task failed {t.Exception}");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public static string PrepareIdentifier(string name, string defaultValue)
    {
        if (name == null)
            return defaultValue;

        var builder = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsLetterOrDigit(c) || c == '_')
                builder.Append(c);
        }

        if (builder.Length == 0)
            return defaultValue;
        else
        {
            if (char.IsDigit(builder[0]))
                builder.Insert(0, '_');
            name = builder.ToString();
        }

        return name;
    }
}
