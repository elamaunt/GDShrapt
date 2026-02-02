using System.Collections.Generic;
using System.Linq;
using GDShrapt.Abstractions;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Extended runtime provider that supports project-level type resolution
    /// and runtime information injection.
    /// </summary>
    public interface IGDProjectRuntimeProvider : IGDRuntimeProvider
    {
        /// <summary>
        /// Resolves a script path to type information.
        /// Used for extends "res://path/to/script.gd" and preload().
        /// </summary>
        /// <param name="scriptPath">The resource path (e.g., "res://scripts/player.gd")</param>
        /// <returns>Type information for the script, or null if not found</returns>
        GDScriptTypeInfo GetScriptType(string scriptPath);

        /// <summary>
        /// Gets type information for a class by its global name (class_name declaration).
        /// </summary>
        /// <param name="className">The class_name identifier</param>
        /// <returns>Type information, or null if not found</returns>
        GDScriptTypeInfo GetProjectClass(string className);

        /// <summary>
        /// Gets all known project classes (from class_name declarations).
        /// </summary>
        IEnumerable<GDScriptTypeInfo> GetProjectClasses();

        /// <summary>
        /// Gets all autoloaded singletons in the project.
        /// </summary>
        IEnumerable<GDAutoloadInfo> GetAutoloads();

        /// <summary>
        /// Resolves a preloaded resource type.
        /// </summary>
        /// <param name="resourcePath">The resource path</param>
        /// <returns>The type of the preloaded resource, or null</returns>
        string GetPreloadType(string resourcePath);

        /// <summary>
        /// Checks if a resource exists at the given path.
        /// </summary>
        /// <param name="resourcePath">Resource path (res://...)</param>
        /// <returns>True if resource exists</returns>
        bool ResourceExists(string resourcePath);

        /// <summary>
        /// Gets signal information for a type (including inherited signals).
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <param name="signalName">The signal name</param>
        /// <returns>Signal info or null if not found</returns>
        GDSignalInfo GetSignal(string typeName, string signalName);

        /// <summary>
        /// Gets all signals declared on a type (including inherited).
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>Collection of signal infos</returns>
        IEnumerable<GDSignalInfo> GetSignals(string typeName);

        /// <summary>
        /// Gets signal connections from scene files for a method.
        /// </summary>
        /// <param name="scriptPath">Path to the script being analyzed.</param>
        /// <param name="methodName">The method name to search for.</param>
        /// <returns>Signal connections targeting this method.</returns>
        IEnumerable<GDSceneSignalConnection> GetSignalConnectionsForMethod(string scriptPath, string methodName);
    }

    /// <summary>
    /// Type information for a GDScript file in the project.
    /// </summary>
    public class GDScriptTypeInfo
    {
        /// <summary>
        /// The resource path of the script (e.g., "res://scripts/player.gd").
        /// </summary>
        public string ScriptPath { get; set; }

        /// <summary>
        /// The class_name if declared, otherwise null.
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// The base type (from extends declaration).
        /// </summary>
        public string BaseType { get; set; }

        /// <summary>
        /// All members declared in this script.
        /// </summary>
        public IReadOnlyList<GDRuntimeMemberInfo> Members { get; set; }

        /// <summary>
        /// All signals declared in this script.
        /// </summary>
        public IReadOnlyList<GDSignalInfo> Signals { get; set; }

        /// <summary>
        /// All methods declared in this script.
        /// </summary>
        public IReadOnlyList<GDMethodInfo> Methods { get; set; }

        /// <summary>
        /// Inner classes declared in this script.
        /// </summary>
        public IReadOnlyList<GDInnerClassInfo> InnerClasses { get; set; }
    }

    /// <summary>
    /// Information about a signal declaration.
    /// </summary>
    public class GDSignalInfo
    {
        public string Name { get; set; }
        public IReadOnlyList<GDRuntimeParameterInfo> Parameters { get; set; }
    }

    /// <summary>
    /// Information about a method declaration.
    /// </summary>
    public class GDMethodInfo
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public IReadOnlyList<GDRuntimeParameterInfo> Parameters { get; set; }
        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }
    }

    /// <summary>
    /// Information about an inner class.
    /// </summary>
    public class GDInnerClassInfo
    {
        public string Name { get; set; }
        public string BaseType { get; set; }
        public IReadOnlyList<GDRuntimeMemberInfo> Members { get; set; }
        public IReadOnlyList<GDMethodInfo> Methods { get; set; }
    }

    /// <summary>
    /// Information about an autoloaded singleton.
    /// </summary>
    public class GDAutoloadInfo
    {
        /// <summary>
        /// The autoload name (used as singleton accessor).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The path to the script or scene.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The type of the autoloaded resource.
        /// </summary>
        public string TypeName { get; set; }
    }

    /// <summary>
    /// Provider for runtime type injection.
    /// Allows injecting type information at specific points in the code.
    /// </summary>
    public interface IGDRuntimeTypeInjector
    {
        /// <summary>
        /// Injects type information for a specific node in the AST.
        /// Called during type inference to provide runtime-derived type information.
        /// </summary>
        /// <param name="node">The AST node</param>
        /// <param name="context">The current inference context</param>
        /// <returns>The injected type, or null to use default inference</returns>
        string InjectType(GDNode node, GDTypeInjectionContext context);

        /// <summary>
        /// Injects type information for a Variant expression.
        /// Called when the inferred type is "Variant" to try to narrow the type.
        /// </summary>
        /// <param name="expression">The expression with Variant type</param>
        /// <param name="context">The current inference context</param>
        /// <returns>The narrowed type, or "Variant" if unknown</returns>
        string NarrowVariantType(GDExpression expression, GDTypeInjectionContext context);

        /// <summary>
        /// Injects type information for a signal callback.
        /// Provides parameter types for signal connections.
        /// </summary>
        /// <param name="signalName">The signal name</param>
        /// <param name="emitterType">The type emitting the signal</param>
        /// <returns>Parameter types for the callback, or null</returns>
        IReadOnlyList<string> GetSignalParameterTypes(string signalName, string emitterType);

        /// <summary>
        /// Injects method return type based on runtime information.
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <param name="receiverType">The type receiving the method call</param>
        /// <param name="argumentTypes">The types of the arguments</param>
        /// <returns>The return type, or null to use default inference</returns>
        string GetMethodReturnType(string methodName, string receiverType, IReadOnlyList<string> argumentTypes);
    }

    /// <summary>
    /// Information about a signal connection from a scene file.
    /// </summary>
    public class GDSceneSignalConnection
    {
        /// <summary>
        /// Path to the scene file containing this connection.
        /// </summary>
        public string ScenePath { get; set; }

        /// <summary>
        /// The signal name being connected.
        /// </summary>
        public string SignalName { get; set; }

        /// <summary>
        /// Node path of the signal source.
        /// </summary>
        public string SourceNodePath { get; set; }

        /// <summary>
        /// Type of the node emitting the signal.
        /// </summary>
        public string SourceNodeType { get; set; }

        /// <summary>
        /// Line number in the .tscn file.
        /// </summary>
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// Context for type injection.
    /// </summary>
    public class GDTypeInjectionContext
    {
        /// <summary>
        /// The current scope.
        /// </summary>
        public GDScope Scope { get; set; }

        /// <summary>
        /// The script path being analyzed.
        /// </summary>
        public string ScriptPath { get; set; }

        /// <summary>
        /// The method being analyzed (if inside a method).
        /// </summary>
        public string CurrentMethod { get; set; }

        /// <summary>
        /// The class type being analyzed.
        /// </summary>
        public string CurrentClass { get; set; }

        /// <summary>
        /// Additional context data for runtime-specific injection.
        /// </summary>
        public IDictionary<string, object> RuntimeData { get; set; }
    }

    /// <summary>
    /// Composite runtime provider that combines multiple providers.
    /// </summary>
    public class GDCompositeRuntimeProvider : IGDProjectRuntimeProvider
    {
        private readonly List<IGDRuntimeProvider> _providers;
        private readonly List<IGDProjectRuntimeProvider> _projectProviders;

        public GDCompositeRuntimeProvider()
        {
            _providers = new List<IGDRuntimeProvider>();
            _projectProviders = new List<IGDProjectRuntimeProvider>();
        }

        public void AddProvider(IGDRuntimeProvider provider)
        {
            _providers.Add(provider);
            if (provider is IGDProjectRuntimeProvider projectProvider)
                _projectProviders.Add(projectProvider);
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

        public GDRuntimeTypeInfo GetTypeInfo(string typeName)
        {
            foreach (var provider in _providers)
            {
                var info = provider.GetTypeInfo(typeName);
                if (info != null)
                    return info;
            }
            return null;
        }

        public GDRuntimeMemberInfo GetMember(string typeName, string memberName)
        {
            foreach (var provider in _providers)
            {
                var info = provider.GetMember(typeName, memberName);
                if (info != null)
                    return info;
            }
            return null;
        }

        public string GetBaseType(string typeName)
        {
            foreach (var provider in _providers)
            {
                var baseType = provider.GetBaseType(typeName);
                if (baseType != null)
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

        public GDRuntimeFunctionInfo GetGlobalFunction(string functionName)
        {
            foreach (var provider in _providers)
            {
                var info = provider.GetGlobalFunction(functionName);
                if (info != null)
                    return info;
            }
            return null;
        }

        public GDRuntimeTypeInfo GetGlobalClass(string className)
        {
            foreach (var provider in _providers)
            {
                var info = provider.GetGlobalClass(className);
                if (info != null)
                    return info;
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

        public bool IsBuiltinType(string typeName)
        {
            foreach (var provider in _providers)
            {
                if (provider.IsBuiltinType(typeName))
                    return true;
            }
            return false;
        }

        public IReadOnlyList<string> FindTypesWithMethod(string methodName)
        {
            var types = new HashSet<string>();
            foreach (var provider in _providers)
            {
                foreach (var type in provider.FindTypesWithMethod(methodName))
                    types.Add(type);
            }
            return types.ToList();
        }

        // IGDProjectRuntimeProvider implementation

        public GDScriptTypeInfo GetScriptType(string scriptPath)
        {
            foreach (var provider in _projectProviders)
            {
                var info = provider.GetScriptType(scriptPath);
                if (info != null)
                    return info;
            }
            return null;
        }

        public GDScriptTypeInfo GetProjectClass(string className)
        {
            foreach (var provider in _projectProviders)
            {
                var info = provider.GetProjectClass(className);
                if (info != null)
                    return info;
            }
            return null;
        }

        public IEnumerable<GDScriptTypeInfo> GetProjectClasses()
        {
            var seen = new HashSet<string>();
            foreach (var provider in _projectProviders)
            {
                foreach (var cls in provider.GetProjectClasses())
                {
                    if (cls.ScriptPath != null && seen.Add(cls.ScriptPath))
                        yield return cls;
                }
            }
        }

        public IEnumerable<GDAutoloadInfo> GetAutoloads()
        {
            var seen = new HashSet<string>();
            foreach (var provider in _projectProviders)
            {
                foreach (var autoload in provider.GetAutoloads())
                {
                    if (autoload.Name != null && seen.Add(autoload.Name))
                        yield return autoload;
                }
            }
        }

        public string GetPreloadType(string resourcePath)
        {
            foreach (var provider in _projectProviders)
            {
                var type = provider.GetPreloadType(resourcePath);
                if (type != null)
                    return type;
            }
            return null;
        }

        public bool ResourceExists(string resourcePath)
        {
            foreach (var provider in _projectProviders)
            {
                if (provider.ResourceExists(resourcePath))
                    return true;
            }
            return false;
        }

        public GDSignalInfo GetSignal(string typeName, string signalName)
        {
            foreach (var provider in _projectProviders)
            {
                var signal = provider.GetSignal(typeName, signalName);
                if (signal != null)
                    return signal;
            }
            return null;
        }

        public IEnumerable<GDSignalInfo> GetSignals(string typeName)
        {
            var seen = new HashSet<string>();
            foreach (var provider in _projectProviders)
            {
                foreach (var signal in provider.GetSignals(typeName))
                {
                    if (signal.Name != null && seen.Add(signal.Name))
                        yield return signal;
                }
            }
        }

        public IEnumerable<GDSceneSignalConnection> GetSignalConnectionsForMethod(string scriptPath, string methodName)
        {
            foreach (var provider in _projectProviders)
            {
                foreach (var connection in provider.GetSignalConnectionsForMethod(scriptPath, methodName))
                {
                    yield return connection;
                }
            }
        }
    }
}
