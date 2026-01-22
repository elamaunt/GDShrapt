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

/*internal class GDScriptProject : IDisposable, IGDScriptProvider
{
    readonly ConcurrentDictionary<string, GDScriptFile> _maps = new ConcurrentDictionary<string, GDScriptFile>();

        FileSystemWatcher? _scriptsWatcher;
        bool _disposedValue;
        GDSceneTypesProvider? _sceneTypesProvider;

        public GDScriptFile? GetScript(string fullPath) => _maps.GetOrDefault(fullPath);

        public IEnumerable<GDScriptFile> Scripts => _maps.Values;
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

        public GDScriptProject()
        {
            Logger.Debug("Project map building");

            var projectPath = ProjectPath;

            LoadScripts(projectPath);
            WatchScripts(projectPath);

            Logger.Info($"Project loaded: {_maps.Count} scripts");
        }


        private void LoadScripts(string projectPath)
        {
            var allScripts = System.IO.Directory.GetFiles(projectPath, "*.gd", SearchOption.AllDirectories);

            for (int i = 0; i < allScripts.Length; i++)
            {
                var scriptFile = allScripts[i];

                Logger.Debug($"Loading script '{System.IO.Path.GetFileName(scriptFile)}'");

                var map = new GDScriptFile(this, scriptFile);
                _maps.TryAdd(scriptFile, map);
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
            _maps.TryRemove(e.FullPath, out _);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
                return;

            Logger.Debug($"Script changed: {e.Name}");

            if (!System.IO.File.Exists(e.Name))
            {
                _maps.TryRemove(e.FullPath, out _);
                return;
            }

            if (_maps.TryGetValue(e.FullPath, out var map))
            {
                map.Reload();
            }
            else
            {
                var newMap = new GDScriptFile(this, e.FullPath);
                _maps[e.FullPath] = newMap;
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
                var ScriptFile = GetScriptByTypeName(className);
                if (ScriptFile?.SemanticModel != null)
                {
                    var symbol = ScriptFile.SemanticModel.FindSymbol(memberName);
                    if (symbol != null && symbol.IsStatic)
                    {
                        var identifier = (symbol.Declaration as GDIdentifiableClassMember)?.Identifier;
                        return new GDCodePointer()
                        {
                            FullPath = ScriptFile.FullPath,
                            DeclarationIdentifier = identifier
                        };
                    }
                }
            }

            // Search for class_name declarations
            foreach (var ScriptFile in Scripts)
            {
                if (ScriptFile.TypeName == name)
                {
                    return new GDCodePointer()
                    {
                        FullPath = ScriptFile.FullPath,
                        DeclarationIdentifier = ScriptFile.Class?.ClassName?.Identifier
                    };
                }
            }

            Logger.Debug($"Declaration not found: {name}");

            return null;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Logger.Debug($"Script created: {e.Name}");
            var newMap = new GDScriptFile(this, e.FullPath);
            _maps.TryAdd(e.FullPath, newMap);
            newMap.Reload();
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Logger.Debug($"Script renamed: {e.OldName} -> {e.Name}");

            if (_maps.TryGetValue(e.OldFullPath, out var map))
            {
                _maps.TryRemove(e.OldFullPath, out _);
                _maps.TryAdd(e.FullPath, map);
            }
            else
            {
                var newMap = new GDScriptFile(this, e.FullPath);
                _maps.TryAdd(e.FullPath, newMap);
                newMap.Reload();
            }
        }

        public GDScriptFile? GetScriptByTypeName(string type) => _maps.FirstOrDefault(x => x.Value.TypeName == type).Value;

        #region IGDScriptProvider Implementation

        IEnumerable<IGDScriptInfo> IGDScriptProvider.Scripts => Scripts;

        IGDScriptInfo? IGDScriptProvider.GetScriptByTypeName(string typeName) => GetScriptByTypeName(typeName);

        IGDScriptInfo? IGDScriptProvider.GetScriptByPath(string path) => GetScript(path);

        #endregion

        public GDScriptFile? GetScriptByResourcePath(string resourcePath)
        {
            var globalPath = ProjectSettings.GlobalizePath(resourcePath);
            return GetScript(globalPath);
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

        public GDScriptFile? GetScriptByClass(GDClassDeclaration classDecl)
        {
            if (classDecl == null)
                return null;

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

        ~GDScriptProject()
        {
            Dispose(disposing: false);
        }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}*/
