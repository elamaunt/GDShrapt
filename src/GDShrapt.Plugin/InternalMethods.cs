using Godot;
using Godot.Collections;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal static class InternalMethods
{
    public static string ToCsSharpStyle(this string name)
    {
        var builder = new StringBuilder(name);

        int i = 0;
        bool shouldBeUpper = false;
        while(i < builder.Length)
        {
            var ch = builder[i];

            if (ch == '_')
            {
                builder.Remove(i, 1);
                shouldBeUpper = true;
                continue;
            }

            if (i == 0 || shouldBeUpper)
            {
                shouldBeUpper = false;
                builder[i] = char.ToUpperInvariant(ch);
            }

            i++;
        }

        return builder.ToString();
    }

    public static bool CompareMethodNames(string name1, string name2)
    {
        return false;
    }

    public static B GetOrDefault<T,B> (this IDictionary<T, B> self, T key, B defaultValue = default)
    {
        if (self.TryGetValue(key, out B value))
            return value;
        return defaultValue;
    }

    public static T GetOrDefault<T>(this WeakReference<T> self, T defaultValue = default)
        where T : class
    {
        if (self.TryGetTarget(out T value))
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

    public static void SearchForName(List<Godot.Node> resultsContainer, Godot.Node obj, string name)
    {
        foreach (var item in obj.GetSignalList().OfType<Dictionary>())
        {
            if (item["name"].ToString().IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
            {
                resultsContainer.Add(obj);
            }
        }

        foreach (var item in obj.GetPropertyList().OfType<Dictionary>())
        {
            if (item["name"].ToString().IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
            {
                resultsContainer.Add(obj);
            }
        }

        foreach (var item in obj.GetMethodList().OfType<Dictionary>())
        {
            if (item["name"].ToString().IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
            {
                resultsContainer.Add(obj);
            }
        }

        for (int i = 0; i < obj.GetChildCount(); i++)
            SearchForName(resultsContainer, obj.GetChild(i), name);
    }

    public static void PrintAll(GodotObject obj, string name)
    {
        if (obj == null)
            return;

        Logger.Info($"--- {name} signals ---");
        foreach (var item in obj.GetSignalList().OfType<Dictionary>())
            Logger.Info(item["name"].ToString());

        Logger.Info($"--- {name} properties ---");
        foreach (var item in obj.GetPropertyList().OfType<Dictionary>())
            Logger.Info(item["name"].ToString());

        Logger.Info($"--- {name} methods ---");
        foreach (var item in obj.GetMethodList().OfType<Dictionary>())
            Logger.Info(item["name"].ToString());
    }

    public static void PrintSignals(this Node node)
    {
        var signals = node.GetSignalList();
        Logger.Info($"Signals for {node}");

        for (int i = 0; i < signals.Count; i++)
        {
            Logger.Info($"Signal {i}: {signals[i]}");
        }
    }

    public static void PrintParents(this Node node)
    {
        var p = node.GetParent();
        Logger.Info($"Parents for {node}");

        while (p != null)
        {
            Logger.Info($"Parent {p}");
            p = p.GetParent();
        }
    }

    public static void PrintChildren(this Node node)
    {
        Logger.Info($"Children for {node}");

        for (int i = 0; i < node.GetChildCount(); i++)
            Logger.Info($"child {i}: {node.GetChild(i)}");
    }

    public static void PrintChildrenRecursive(this Node node)
    {
        Logger.Info("");

        Logger.Info($"Children for {node}");

        for (int i = 0; i < node.GetChildCount(); i++)
            Logger.Info($"child {i}: {node.GetChild(i)}");

        for (int i = 0; i < node.GetChildCount(); i++)
            PrintChildrenRecursive(node.GetChild(i));
    }

    public static void SwapChild(this Node node, int index, Func<Node, Node> update)
    {
        var child = node.GetChild(index);
        node.RemoveChild(child);
        node.AddChild(update(child));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrintIfFailed(this Task task)
    {
        task.ContinueWith(t =>
        {
            Logger.Info($"Async task failed {t.Exception}");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NoWarning(this Task task)
    {
    }

    public static T TryPopOrDefault<T>(this Stack<T> self, T defaultValue = default)
    {
        if (self.Count > 0)
            return self.Pop();
        return defaultValue;
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

    public static void ClearChildren(this Node node)
    {
        var count = node.GetChildCount();
        for (int i = 0; i < count; i++)
            node.RemoveChild(node.GetChild(0));
    }

    public static B GetOrAdd<T, B>(this IDictionary<T,B> self, T key)
        where B : new()
    {
        if (self.TryGetValue(key, out B value))
            return value;
        return self[key] = new B();
    }

    public static B GetOrAdd<T, B>(this IDictionary<T, B> self, T key, Func<T,B> factory)
    {
        if (self.TryGetValue(key, out B value))
            return value;
        return self[key] = factory(key);
    }

    public static T? GetParentOfType<T>(this GDNode node) where T : GDNode
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is T typed)
                return typed;
            parent = parent.Parent;
        }
        return null;
    }

    public static void ConnectWith<T>(this TaskCompletionSource<T> self, TaskCompletionSource<T> other)
    {
        self.Task.ContinueWith(t =>
        {
            if (t.Exception != null)
                other.TrySetException(t.Exception);
            else
                other.TrySetResult(t.Result);
        });
    }
}
