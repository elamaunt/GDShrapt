using System.Collections.Generic;
using GDShrapt.Abstractions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Caching wrapper for IGDRuntimeProvider.
    /// Caches type lookups, member info, and function info for better performance.
    /// </summary>
    public class GDCachingRuntimeProvider : IGDRuntimeProvider
    {
        private readonly IGDRuntimeProvider _inner;
        private readonly Dictionary<string, GDRuntimeTypeInfo> _typeCache;
        private readonly Dictionary<(string, string), GDRuntimeMemberInfo> _memberCache;
        private readonly Dictionary<string, GDRuntimeFunctionInfo> _functionCache;
        private readonly Dictionary<string, GDRuntimeTypeInfo> _globalClassCache;
        private readonly Dictionary<string, bool> _knownTypeCache;
        private readonly Dictionary<string, string> _baseTypeCache;
        private readonly Dictionary<(string, string), bool> _assignableCache;
        private readonly Dictionary<string, bool> _builtInCache;

        /// <summary>
        /// Creates a caching wrapper around the given provider.
        /// </summary>
        public GDCachingRuntimeProvider(IGDRuntimeProvider inner)
        {
            _inner = inner;
            _typeCache = new Dictionary<string, GDRuntimeTypeInfo>();
            _memberCache = new Dictionary<(string, string), GDRuntimeMemberInfo>();
            _functionCache = new Dictionary<string, GDRuntimeFunctionInfo>();
            _globalClassCache = new Dictionary<string, GDRuntimeTypeInfo>();
            _knownTypeCache = new Dictionary<string, bool>();
            _baseTypeCache = new Dictionary<string, string>();
            _assignableCache = new Dictionary<(string, string), bool>();
            _builtInCache = new Dictionary<string, bool>();
        }

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        public void ClearCache()
        {
            _typeCache.Clear();
            _memberCache.Clear();
            _functionCache.Clear();
            _globalClassCache.Clear();
            _knownTypeCache.Clear();
            _baseTypeCache.Clear();
            _assignableCache.Clear();
            _builtInCache.Clear();
        }

        public bool IsKnownType(string typeName)
        {
            if (typeName == null) return false;

            if (_knownTypeCache.TryGetValue(typeName, out var result))
                return result;

            result = _inner.IsKnownType(typeName);
            _knownTypeCache[typeName] = result;
            return result;
        }

        public GDRuntimeTypeInfo GetTypeInfo(string typeName)
        {
            if (typeName == null) return null;

            if (_typeCache.TryGetValue(typeName, out var result))
                return result;

            result = _inner.GetTypeInfo(typeName);
            _typeCache[typeName] = result;
            return result;
        }

        public GDRuntimeMemberInfo GetMember(string typeName, string memberName)
        {
            if (typeName == null || memberName == null) return null;

            var key = (typeName, memberName);
            if (_memberCache.TryGetValue(key, out var result))
                return result;

            result = _inner.GetMember(typeName, memberName);
            _memberCache[key] = result;
            return result;
        }

        public string GetBaseType(string typeName)
        {
            if (typeName == null) return null;

            if (_baseTypeCache.TryGetValue(typeName, out var result))
                return result;

            result = _inner.GetBaseType(typeName);
            _baseTypeCache[typeName] = result;
            return result;
        }

        public bool IsAssignableTo(string sourceType, string targetType)
        {
            if (sourceType == null || targetType == null) return false;

            var key = (sourceType, targetType);
            if (_assignableCache.TryGetValue(key, out var result))
                return result;

            result = _inner.IsAssignableTo(sourceType, targetType);
            _assignableCache[key] = result;
            return result;
        }

        public GDRuntimeFunctionInfo GetGlobalFunction(string functionName)
        {
            if (functionName == null) return null;

            if (_functionCache.TryGetValue(functionName, out var result))
                return result;

            result = _inner.GetGlobalFunction(functionName);
            _functionCache[functionName] = result;
            return result;
        }

        public GDRuntimeTypeInfo GetGlobalClass(string className)
        {
            if (className == null) return null;

            if (_globalClassCache.TryGetValue(className, out var result))
                return result;

            result = _inner.GetGlobalClass(className);
            _globalClassCache[className] = result;
            return result;
        }

        public bool IsBuiltIn(string identifier)
        {
            if (identifier == null) return false;

            if (_builtInCache.TryGetValue(identifier, out var result))
                return result;

            result = _inner.IsBuiltIn(identifier);
            _builtInCache[identifier] = result;
            return result;
        }

        public IEnumerable<string> GetAllTypes()
        {
            // Delegate to inner provider - no caching needed for enumeration
            return _inner.GetAllTypes();
        }

        public bool IsBuiltinType(string typeName)
        {
            // Delegate to inner provider - caching could be added if needed
            return _inner.IsBuiltinType(typeName);
        }

        public IReadOnlyList<string> FindTypesWithMethod(string methodName)
        {
            // Delegate to inner provider - caching could be added if needed
            return _inner.FindTypesWithMethod(methodName);
        }
    }
}
