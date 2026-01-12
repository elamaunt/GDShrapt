using GDShrapt.Reader;
using GDShrapt.Semantics;
using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GDShrapt.Plugin;

internal class GDProjectMap : IDisposable, IGDScriptProvider
    {
        readonly ConcurrentDictionary<GDPluginScriptReference, GDScriptMapUIBinding> _bindings = new ConcurrentDictionary<GDPluginScriptReference, GDScriptMapUIBinding>();
        FileSystemWatcher? _scriptsWatcher;
        bool _disposedValue;
        GDSceneTypesProvider? _sceneTypesProvider;

        public GDScriptMap? GetScriptMap(string fullPath) => _bindings.GetOrDefault(new GDPluginScriptReference(fullPath))?.ScriptMap;

        public GDScriptMap? GetScriptMap(GDPluginScriptReference reference) => _bindings.GetOrDefault(reference)?.ScriptMap;

        public GDScriptMapUIBinding? GetBinding(string fullPath) => _bindings.GetOrDefault(new GDPluginScriptReference(fullPath));

        public GDScriptMapUIBinding? GetBinding(GDPluginScriptReference reference) => _bindings.GetOrDefault(reference);

        public IEnumerable<GDScriptMap> Scripts => _bindings.Values.Select(b => b.ScriptMap);
        public IEnumerable<GDScriptMapUIBinding> Bindings => _bindings.Values;
        public string ProjectPath => ProjectSettings.GlobalizePath("res://");

        /// <summary>
        /// Provider for scene file type information. Lazily initialized.
        /// Uses GDSceneTypesProvider from GDShrapt.Semantics with logging support.
        /// </summary>
        public GDSceneTypesProvider SceneTypesProvider
        {
            get
            {
                if (_sceneTypesProvider == null)
                {
                    _sceneTypesProvider = new GDSceneTypesProvider(
                        ProjectPath,
                        fileSystem: null,
                        logger: SemanticLoggerAdapter.Instance);
                    _sceneTypesProvider.ReloadAllScenes();
                }
                return _sceneTypesProvider;
            }
        }

        public GDProjectMap()
        {
            Logger.Debug("Project map building");

            var projectPath = ProjectPath;

            LoadScripts(projectPath);
            WatchScripts(projectPath);

            Logger.Info($"Project loaded: {_bindings.Count} scripts");
        }

        public GDProjectMap(params string[] contents)
        {
            Logger.Debug("Project map building with custom content");

            for (int i = 0; i < contents.Length; i++)
            {
                var reference = new GDPluginScriptReference(i.ToString());
                var map = new GDScriptMap(this, reference);
                var binding = new GDScriptMapUIBinding(map);
                _bindings.TryAdd(reference, binding);
                map.Reload(contents[i]);
            }

            Logger.Debug($"Project map built: {contents.Length} scripts");
        }

        private void LoadScripts(string projectPath)
        {
            var allScripts = System.IO.Directory.GetFiles(projectPath, "*.gd", SearchOption.AllDirectories);

            for (int i = 0; i < allScripts.Length; i++)
            {
                var scriptFile = allScripts[i];

                Logger.Debug($"Loading script '{System.IO.Path.GetFileName(scriptFile)}'");

                var reference = new GDPluginScriptReference(scriptFile);
                var map = new GDScriptMap(this, reference);
                var binding = new GDScriptMapUIBinding(map);
                _bindings.TryAdd(reference, binding);
                map.Reload();
            }
        }

        private void WatchScripts(string path)
        {
            Logger.Debug("Setting up file watcher");
            _scriptsWatcher = new FileSystemWatcher();
            _scriptsWatcher.Path = path;
            _scriptsWatcher.Filter = "*.gd";
            _scriptsWatcher.Renamed += OnRenamed;
            _scriptsWatcher.Created += OnCreated;
            _scriptsWatcher.Changed += OnChanged;
            _scriptsWatcher.Deleted += OnDeleted;
            _scriptsWatcher.EnableRaisingEvents = true;
            _scriptsWatcher.IncludeSubdirectories = true;
            Logger.Debug("File watcher installed");
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Logger.Debug($"Script deleted: {e.Name}");
            _bindings.TryRemove(new GDPluginScriptReference(e.Name), out _);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            Logger.Debug($"Script changed: {e.Name}");

            if (!System.IO.File.Exists(e.Name))
            {
                _bindings.TryRemove(new GDPluginScriptReference(e.Name), out _);
                return;
            }

            var reference = new GDPluginScriptReference(e.Name);
            if (_bindings.TryGetValue(reference, out var binding))
            {
                binding.ScriptMap.Reload();
            }
            else
            {
                var newMap = new GDScriptMap(this, reference);
                var newBinding = new GDScriptMapUIBinding(newMap);
                _bindings[reference] = newBinding;
                newMap.Reload();
            }
        }

        internal GDCodePointer? FindStaticDeclarationIdentifier(string name)
        {
            Logger.Debug($"FindStaticDeclarationIdentifier: {name}");

            // Support "ClassName.member_name" format for static members
            var dotIndex = name.IndexOf('.');
            if (dotIndex > 0)
            {
                var className = name.Substring(0, dotIndex);
                var memberName = name.Substring(dotIndex + 1);
                var scriptMap = GetScriptMapByTypeName(className);
                if (scriptMap?.Analyzer != null)
                {
                    var symbol = scriptMap.Analyzer.FindSymbol(memberName);
                    if (symbol != null && symbol.IsStatic)
                    {
                        var identifier = (symbol.Declaration as GDIdentifiableClassMember)?.Identifier;
                        return new GDCodePointer()
                        {
                            ScriptReference = scriptMap.Reference,
                            DeclarationIdentifier = identifier
                        };
                    }
                }
            }

            // Search for class_name declarations
            foreach (var scriptMap in Scripts)
            {
                if (scriptMap.TypeName == name)
                {
                    return new GDCodePointer()
                    {
                        ScriptReference = scriptMap.Reference,
                        DeclarationIdentifier = scriptMap.Class?.ClassName?.Identifier
                    };
                }
            }

            Logger.Debug($"Declaration not found: {name}");

            return null;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Logger.Debug($"Script created: {e.Name}");
            var reference = new GDPluginScriptReference(e.Name);
            var newMap = new GDScriptMap(this, reference);
            var newBinding = new GDScriptMapUIBinding(newMap);
            _bindings.TryAdd(reference, newBinding);
            newMap.Reload();
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Logger.Debug($"Script renamed: {e.OldName} -> {e.Name}");

            var oldReference = new GDPluginScriptReference(e.OldName);

            if (_bindings.TryGetValue(oldReference, out var binding))
            {
                _bindings.TryRemove(oldReference, out _);
                var newReference = new GDPluginScriptReference(e.Name);
                _bindings.TryAdd(newReference, binding);
                binding.ScriptMap.ChangeReference(newReference);
            }
            else
            {
                var newReference = new GDPluginScriptReference(e.Name);
                var newMap = new GDScriptMap(this, newReference);
                var newBinding = new GDScriptMapUIBinding(newMap);
                _bindings.TryAdd(newReference, newBinding);
                newMap.Reload();
            }
        }

        public GDScriptMap? GetScriptMapByTypeName(string type) => _bindings.FirstOrDefault(x => x.Value.ScriptMap.TypeName == type).Value?.ScriptMap;

        public GDScriptMapUIBinding? GetBindingByTypeName(string type) => _bindings.FirstOrDefault(x => x.Value.ScriptMap.TypeName == type).Value;

        #region IGDScriptProvider Implementation

        IEnumerable<IGDScriptInfo> IGDScriptProvider.Scripts => Scripts;

        IGDScriptInfo? IGDScriptProvider.GetScriptByTypeName(string typeName) => GetScriptMapByTypeName(typeName);

        IGDScriptInfo? IGDScriptProvider.GetScriptByPath(string path) => GetScriptMap(path);

        #endregion

        public GDScriptMap? GetScriptMapByResourcePath(string resourcePath)
        {
            var globalPath = ProjectSettings.GlobalizePath(resourcePath);
            return GetScriptMap(new GDPluginScriptReference(globalPath));
        }

        public GDScriptMapUIBinding? GetBindingByResourcePath(string resourcePath)
        {
            var globalPath = ProjectSettings.GlobalizePath(resourcePath);
            return GetBinding(new GDPluginScriptReference(globalPath));
        }

        /// <summary>
        /// Creates a runtime provider for type resolution across the project.
        /// </summary>
        internal IGDRuntimeProvider CreateRuntimeProvider()
        {
            return new GDProjectRuntimeProvider(this);
        }

        /// <summary>
        /// Creates a fully configured type resolver with all providers including autoloads.
        /// Mirrors GDScriptProject.CreateTypeResolver() pattern.
        /// </summary>
        public GDTypeResolver CreateTypeResolver()
        {
            var godotTypesProvider = new GDGodotTypesProvider();
            var projectTypesProvider = new GDProjectTypesProvider(this);
            projectTypesProvider.RebuildCache();

            // Load autoloads from project.godot
            var projectGodotPath = System.IO.Path.Combine(ProjectPath, "project.godot");
            var autoloads = GDGodotProjectParser.ParseAutoloads(projectGodotPath);
            var autoloadsProvider = new GDAutoloadsProvider(autoloads, this);

            return new GDTypeResolver(
                godotTypesProvider,
                projectTypesProvider,
                autoloadsProvider,
                SceneTypesProvider,
                this, // IGDScriptProvider for preload type inference
                SemanticLoggerAdapter.Instance);
        }

        public GDScriptMap? GetScriptMapByClass(GDClassDeclaration classDecl)
        {
            if (classDecl == null) return null;
            return _bindings.FirstOrDefault(x => x.Value.ScriptMap.Class == classDecl).Value?.ScriptMap;
        }

        public GDScriptMapUIBinding? GetBindingByClass(GDClassDeclaration classDecl)
        {
            if (classDecl == null) return null;
            return _bindings.FirstOrDefault(x => x.Value.ScriptMap.Class == classDecl).Value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                    _bindings.Clear();

                _scriptsWatcher?.Dispose();
                _scriptsWatcher = null;
                _disposedValue = true;
            }
        }

        ~GDProjectMap()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: false);
        }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
