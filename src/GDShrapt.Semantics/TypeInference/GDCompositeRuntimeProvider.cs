using GDShrapt.Reader;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Composite runtime provider that combines multiple type sources.
/// Queries providers in order and returns first non-null result.
/// </summary>
public class GDCompositeRuntimeProvider : IGDRuntimeProvider
{
    private readonly IGDRuntimeProvider[] _providers;

    public GDCompositeRuntimeProvider(params IGDRuntimeProvider?[] providers)
    {
        _providers = providers.Where(p => p != null).Cast<IGDRuntimeProvider>().ToArray();
    }

    public bool IsKnownType(string typeName)
    {
        foreach (var provider in _providers)
        {
            if (provider.IsKnownType(typeName))
                return true;
        }
        return false;
    }

    public GDRuntimeTypeInfo? GetTypeInfo(string typeName)
    {
        foreach (var provider in _providers)
        {
            var info = provider.GetTypeInfo(typeName);
            if (info != null)
                return info;
        }
        return null;
    }

    public GDRuntimeMemberInfo? GetMember(string typeName, string memberName)
    {
        foreach (var provider in _providers)
        {
            var member = provider.GetMember(typeName, memberName);
            if (member != null)
                return member;
        }
        return null;
    }

    public string? GetBaseType(string typeName)
    {
        foreach (var provider in _providers)
        {
            var baseType = provider.GetBaseType(typeName);
            if (!string.IsNullOrEmpty(baseType))
                return baseType;
        }
        return null;
    }

    public bool IsAssignableTo(string sourceType, string targetType)
    {
        foreach (var provider in _providers)
        {
            if (provider.IsAssignableTo(sourceType, targetType))
                return true;
        }
        return false;
    }

    public GDRuntimeFunctionInfo? GetGlobalFunction(string name)
    {
        foreach (var provider in _providers)
        {
            var func = provider.GetGlobalFunction(name);
            if (func != null)
                return func;
        }
        return null;
    }

    public GDRuntimeTypeInfo? GetGlobalClass(string className)
    {
        foreach (var provider in _providers)
        {
            var cls = provider.GetGlobalClass(className);
            if (cls != null)
                return cls;
        }
        return null;
    }

    public bool IsBuiltIn(string identifier)
    {
        foreach (var provider in _providers)
        {
            if (provider.IsBuiltIn(identifier))
                return true;
        }
        return false;
    }

    public IEnumerable<string> GetAllTypes()
    {
        var types = new HashSet<string>();
        foreach (var provider in _providers)
        {
            foreach (var type in provider.GetAllTypes())
                types.Add(type);
        }
        return types;
    }
}
