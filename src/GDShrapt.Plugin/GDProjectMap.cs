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
        readonly ConcurrentDictionary<ScriptReference, GDScriptMap> _maps = new ConcurrentDictionary<ScriptReference, GDScriptMap>();
        FileSystemWatcher _scriptsWatcher;
        bool _disposedValue;
        GDSceneTypesProvider _sceneTypesProvider;

        public GDScriptMap GetScriptMap(string fullPath) => _maps.GetOrDefault(new ScriptReference(fullPath));

        public GDScriptMap GetScriptMap(ScriptReference reference) => _maps.GetOrDefault(reference);

        public IEnumerable<GDScriptMap> Scripts => _maps.Values;
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

            Logger.Info($"Project loaded: {_maps.Count} scripts");
        }

        public GDProjectMap(params string[] contents)
        {
            Logger.Debug("Project map building with custom content");

            for (int i = 0; i < contents.Length; i++)
            {
                var reference = new ScriptReference(i.ToString());
                var map = new GDScriptMap(this, reference);
                _maps.TryAdd(reference, map);
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

                var reference = new ScriptReference(scriptFile);
                var map = new GDScriptMap(this, reference);
                _maps.TryAdd(reference, map);
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
            _maps.TryRemove(new ScriptReference(e.Name), out var map);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            Logger.Debug($"Script changed: {e.Name}");

            if (!System.IO.File.Exists(e.Name))
            {
                _maps.TryRemove(new ScriptReference(e.Name), out var removedMap);
                return;
            }

            var reference = new ScriptReference(e.Name);
            if (_maps.TryGetValue(reference, out var map))
            {
                map.Reload();
            }
            else
            {
                var newMap = new GDScriptMap(this, reference);
                _maps[reference] = newMap;
                newMap.Reload();
            }
        }

        internal CodePointer FindStaticDeclarationIdentifier(string name)
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
                        return new CodePointer()
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
                    return new CodePointer()
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
            var reference = new ScriptReference(e.Name);
            var newMap = new GDScriptMap(this, reference);
            _maps.TryAdd(reference, newMap);
            newMap.Reload();
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Logger.Debug($"Script renamed: {e.OldName} -> {e.Name}");

            var oldReference = new ScriptReference(e.OldName);

            if (_maps.TryGetValue(oldReference, out var map))
            {
                _maps.TryRemove(oldReference, out var removedMap);
                var newReference = new ScriptReference(e.Name);
                _maps.TryAdd(newReference, map);
                map.ChangeReference(newReference);
            }
            else
            {
                var newReference = new ScriptReference(e.Name);
                map = new GDScriptMap(this, newReference);
                _maps.TryAdd(newReference, map);
                map.Reload();
            }
        }

        public GDScriptMap GetScriptMapByTypeName(string type) => _maps.FirstOrDefault(x => x.Value.TypeName == type).Value;

        #region IGDScriptProvider Implementation

        IEnumerable<IGDScriptInfo> IGDScriptProvider.Scripts =>
            _maps.Values.Select(m => new GDScriptMapAdapter(m));

        IGDScriptInfo? IGDScriptProvider.GetScriptByTypeName(string typeName) =>
            GetScriptMapByTypeName(typeName) is { } map ? new GDScriptMapAdapter(map) : null;

        IGDScriptInfo? IGDScriptProvider.GetScriptByPath(string path) =>
            GetScriptMap(path) is { } map ? new GDScriptMapAdapter(map) : null;

        #endregion

        public GDScriptMap GetScriptMapByResourcePath(string resourcePath)
        {
            var globalPath = ProjectSettings.GlobalizePath(resourcePath);
            return GetScriptMap(new ScriptReference(globalPath));
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
                SemanticLoggerAdapter.Instance);
        }

        public GDScriptMap GetScriptMapByClass(GDClassDeclaration classDecl)
        {
            if (classDecl == null) return null;
            return _maps.FirstOrDefault(x => x.Value.Class == classDecl).Value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                    _maps.Clear();

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
