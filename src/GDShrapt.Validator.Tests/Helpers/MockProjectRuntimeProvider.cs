using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;

namespace GDShrapt.Reader.Tests.Helpers
{
    /// <summary>
    /// Mock implementation of IGDProjectRuntimeProvider for testing.
    /// Allows explicit registration of resources, scripts, and signals for validation tests.
    /// </summary>
    internal class MockProjectRuntimeProvider : IGDProjectRuntimeProvider
    {
        private readonly HashSet<string> _resources = new HashSet<string>();
        private readonly Dictionary<string, GDScriptTypeInfo> _scripts = new Dictionary<string, GDScriptTypeInfo>();
        private readonly Dictionary<string, GDScriptTypeInfo> _classesByName = new Dictionary<string, GDScriptTypeInfo>();
        private readonly Dictionary<string, List<GDSignalInfo>> _typeSignals = new Dictionary<string, List<GDSignalInfo>>();
        private readonly List<GDAutoloadInfo> _autoloads = new List<GDAutoloadInfo>();
        private readonly IGDRuntimeProvider _builtInProvider = GDDefaultRuntimeProvider.Instance;

        #region Configuration Methods

        /// <summary>
        /// Registers a resource as existing at the given path.
        /// </summary>
        public void AddResource(string path)
        {
            _resources.Add(path);
        }

        /// <summary>
        /// Registers a script with type information.
        /// </summary>
        public void AddScript(string path, GDScriptTypeInfo info)
        {
            _scripts[path] = info;
            if (!string.IsNullOrEmpty(info.ClassName))
            {
                _classesByName[info.ClassName] = info;
            }
        }

        /// <summary>
        /// Registers a signal for a type.
        /// </summary>
        public void AddSignal(string typeName, GDSignalInfo signal)
        {
            if (!_typeSignals.TryGetValue(typeName ?? "self", out var signals))
            {
                signals = new List<GDSignalInfo>();
                _typeSignals[typeName ?? "self"] = signals;
            }
            signals.Add(signal);
        }

        /// <summary>
        /// Registers an autoload singleton.
        /// </summary>
        public void AddAutoload(GDAutoloadInfo autoload)
        {
            _autoloads.Add(autoload);
        }

        #endregion

        #region IGDRuntimeProvider Implementation

        public bool IsKnownType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            if (_builtInProvider.IsKnownType(typeName))
                return true;

            return _classesByName.ContainsKey(typeName);
        }

        public GDRuntimeTypeInfo GetTypeInfo(string typeName)
        {
            return _builtInProvider.GetTypeInfo(typeName);
        }

        public GDRuntimeMemberInfo GetMember(string typeName, string memberName)
        {
            return _builtInProvider.GetMember(typeName, memberName);
        }

        public string GetBaseType(string typeName)
        {
            if (_classesByName.TryGetValue(typeName, out var scriptInfo))
            {
                return scriptInfo.BaseType;
            }
            return _builtInProvider.GetBaseType(typeName);
        }

        public bool IsAssignableTo(string sourceType, string targetType)
        {
            return _builtInProvider.IsAssignableTo(sourceType, targetType);
        }

        public GDRuntimeFunctionInfo GetGlobalFunction(string functionName)
        {
            return _builtInProvider.GetGlobalFunction(functionName);
        }

        public GDRuntimeTypeInfo GetGlobalClass(string className)
        {
            return _builtInProvider.GetGlobalClass(className);
        }

        public bool IsBuiltIn(string identifier)
        {
            return _builtInProvider.IsBuiltIn(identifier);
        }

        public IEnumerable<string> GetAllTypes()
        {
            // Combine built-in types with registered project classes
            foreach (var type in _builtInProvider.GetAllTypes())
                yield return type;

            foreach (var className in _classesByName.Keys)
                yield return className;
        }

        public bool IsBuiltinType(string typeName)
        {
            return _builtInProvider.IsBuiltinType(typeName);
        }

        public IReadOnlyList<string> FindTypesWithMethod(string methodName)
        {
            return _builtInProvider.FindTypesWithMethod(methodName);
        }

        #endregion

        #region IGDProjectRuntimeProvider Implementation

        public GDScriptTypeInfo GetScriptType(string scriptPath)
        {
            if (string.IsNullOrEmpty(scriptPath))
                return null;

            _scripts.TryGetValue(scriptPath, out var info);
            return info;
        }

        public GDScriptTypeInfo GetProjectClass(string className)
        {
            if (string.IsNullOrEmpty(className))
                return null;

            _classesByName.TryGetValue(className, out var info);
            return info;
        }

        public IEnumerable<GDScriptTypeInfo> GetProjectClasses()
        {
            return _classesByName.Values;
        }

        public IEnumerable<GDAutoloadInfo> GetAutoloads()
        {
            return _autoloads;
        }

        public string GetPreloadType(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return null;

            // Return script type name if it's a registered script
            if (_scripts.TryGetValue(resourcePath, out var scriptInfo))
            {
                return scriptInfo.ClassName ?? scriptInfo.BaseType ?? "Resource";
            }

            // Fallback to extension-based type inference
            if (resourcePath.EndsWith(".tscn") || resourcePath.EndsWith(".scn"))
                return "PackedScene";
            if (resourcePath.EndsWith(".tres") || resourcePath.EndsWith(".res"))
                return "Resource";
            if (resourcePath.EndsWith(".png") || resourcePath.EndsWith(".jpg") ||
                resourcePath.EndsWith(".webp") || resourcePath.EndsWith(".svg"))
                return "Texture2D";

            return null;
        }

        public bool ResourceExists(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return false;

            return _resources.Contains(resourcePath) || _scripts.ContainsKey(resourcePath);
        }

        public GDSignalInfo GetSignal(string typeName, string signalName)
        {
            if (string.IsNullOrEmpty(signalName))
                return null;

            var key = typeName ?? "self";
            if (_typeSignals.TryGetValue(key, out var signals))
            {
                var signal = signals.FirstOrDefault(s => s.Name == signalName);
                if (signal != null)
                    return signal;
            }

            // Check built-in type signals
            if (!string.IsNullOrEmpty(typeName))
            {
                var typeInfo = _builtInProvider.GetTypeInfo(typeName);
                if (typeInfo?.Members != null)
                {
                    var signalMember = typeInfo.Members.FirstOrDefault(m =>
                        m.Kind == GDRuntimeMemberKind.Signal && m.Name == signalName);
                    if (signalMember != null)
                    {
                        return new GDSignalInfo
                        {
                            Name = signalName,
                            Parameters = signalMember.Parameters
                        };
                    }
                }
            }

            return null;
        }

        public IEnumerable<GDSignalInfo> GetSignals(string typeName)
        {
            var key = typeName ?? "self";
            if (_typeSignals.TryGetValue(key, out var signals))
            {
                foreach (var signal in signals)
                {
                    yield return signal;
                }
            }

            // Also include built-in type signals
            if (!string.IsNullOrEmpty(typeName))
            {
                var typeInfo = _builtInProvider.GetTypeInfo(typeName);
                if (typeInfo?.Members != null)
                {
                    foreach (var member in typeInfo.Members)
                    {
                        if (member.Kind == GDRuntimeMemberKind.Signal)
                        {
                            yield return new GDSignalInfo
                            {
                                Name = member.Name,
                                Parameters = member.Parameters
                            };
                        }
                    }
                }
            }
        }

        public IEnumerable<GDSceneSignalConnection> GetSignalConnectionsForMethod(string scriptPath, string methodName)
        {
            // No scene connections in mock by default
            yield break;
        }

        #endregion

        #region Type Traits - delegated to built-in provider

        public bool IsNumericType(string typeName) => _builtInProvider.IsNumericType(typeName);
        public bool IsIterableType(string typeName) => _builtInProvider.IsIterableType(typeName);
        public bool IsIndexableType(string typeName) => _builtInProvider.IsIndexableType(typeName);
        public bool IsNullableType(string typeName) => _builtInProvider.IsNullableType(typeName);
        public bool IsVectorType(string typeName) => _builtInProvider.IsVectorType(typeName);
        public bool IsContainerType(string typeName) => _builtInProvider.IsContainerType(typeName);
        public bool IsPackedArrayType(string typeName) => _builtInProvider.IsPackedArrayType(typeName);
        public string GetFloatVectorVariant(string integerVectorType) => _builtInProvider.GetFloatVectorVariant(integerVectorType);
        public string GetPackedArrayElementType(string packedArrayType) => _builtInProvider.GetPackedArrayElementType(packedArrayType);
        public string ResolveOperatorResult(string leftType, string operatorName, string rightType) => _builtInProvider.ResolveOperatorResult(leftType, operatorName, rightType);
        public IReadOnlyList<string> GetTypesWithOperator(string operatorName) => _builtInProvider.GetTypesWithOperator(operatorName);
        public IReadOnlyList<string> GetTypesWithNonZeroCollisionLayer() => _builtInProvider.GetTypesWithNonZeroCollisionLayer();
        public IReadOnlyList<GDCollisionLayerInfo> GetCollisionLayerDetails() => _builtInProvider.GetCollisionLayerDetails();
        public IReadOnlyList<string> GetTypesWithNonZeroAvoidanceLayers() => _builtInProvider.GetTypesWithNonZeroAvoidanceLayers();
        public IReadOnlyList<GDAvoidanceLayerInfo> GetAvoidanceLayerDetails() => _builtInProvider.GetAvoidanceLayerDetails();

        #endregion
    }
}
