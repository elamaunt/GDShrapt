using System;
using System.Collections.Generic;

namespace GDShrapt.Semantics;

internal class GDExpressionDispatchRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<string, List<GDExpressionDispatchEntry>> _byMethodName = new(StringComparer.Ordinal);

    public void Register(GDExpressionDispatchEntry entry)
    {
        lock (_lock)
        {
            if (!_byMethodName.TryGetValue(entry.ResolvedMethodName, out var list))
            {
                list = new List<GDExpressionDispatchEntry>();
                _byMethodName[entry.ResolvedMethodName] = list;
            }
            list.Add(entry);
        }
    }

    public bool IsMethodReferenced(string methodName, IReadOnlyList<string> receiverTypeNames)
    {
        lock (_lock)
        {
            if (!_byMethodName.TryGetValue(methodName, out var entries))
                return false;

            foreach (var entry in entries)
            {
                if (entry.ReceiverType == null)
                    return true;

                foreach (var name in receiverTypeNames)
                {
                    if (string.Equals(entry.ReceiverType, name, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }
    }
}
