using System.Diagnostics;
using GDShrapt.Abstractions;
using GDShrapt.Linter;
using GDShrapt.Reader;
using GDShrapt.Semantics.Incremental.Cache;
using GDShrapt.Semantics.Incremental.Results;
using GDShrapt.Semantics.Incremental.Tracking;

namespace GDShrapt.Semantics.Incremental;

/// <summary>
/// Incremental analyzer that only processes changed files.
/// </summary>
public class GDIncrementalAnalyzer : IGDIncrementalAnalyzer
{
    private readonly IGDAnalysisCache _cache;
    private readonly GDFileChangeTracker _tracker;
    private readonly GDDependencyGraph _dependencies;
    private readonly string _projectPath;
    private readonly GDValidator _validator;
    private readonly GDLinter _linter;
    private readonly GDValidationOptions _validationOptions;
    private readonly GDLinterOptions _linterOptions;
    private readonly IGDLogger? _logger;

    private const string ToolVersion = "1.0.0";

    /// <summary>
    /// Creates a new incremental analyzer.
    /// </summary>
    /// <param name="projectPath">Path to the project root.</param>
    /// <param name="config">Optional incremental analysis configuration.</param>
    /// <param name="validationOptions">Optional validation options. If null, uses default.</param>
    /// <param name="linterOptions">Optional linter options. If null, uses default.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public GDIncrementalAnalyzer(
        string projectPath,
        GDIncrementalConfig? config = null,
        GDValidationOptions? validationOptions = null,
        GDLinterOptions? linterOptions = null,
        IGDLogger? logger = null)
    {
        _projectPath = projectPath;
        _logger = logger;

        config ??= new GDIncrementalConfig();

        var cacheDir = config.GetEffectiveCacheDirectory(projectPath);

        _cache = config.PersistCache
            ? new GDFileCache(cacheDir, config.MaxCacheSizeMb, logger)
            : new GDMemoryCache();

        _tracker = new GDFileChangeTracker();
        _dependencies = new GDDependencyGraph();

        // Initialize validator and linter
        _validator = new GDValidator();
        _linter = new GDLinter(linterOptions ?? GDLinterOptions.Default);
        _validationOptions = validationOptions ?? GDValidationOptions.Default;
        _linterOptions = linterOptions ?? GDLinterOptions.Default;

        // Load existing cache from disk if using file cache
        if (_cache is GDFileCache fileCache)
        {
            fileCache.LoadFromDisk();
        }
    }

    /// <inheritdoc />
    public GDIncrementalAnalysisResult Analyze(
        GDScriptProject project,
        GDIncrementalConfig config,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = GDIncrementalAnalysisResult.Succeeded();

        try
        {
            // Detect changes
            var changes = _tracker.DetectChanges(project);

            // Get all files that need analysis (changed + dependents)
            var filesToAnalyze = GetFilesToAnalyze(changes, project);

            // Analyze files in parallel
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = config.MaxParallelism,
                CancellationToken = cancellationToken
            };

            var diagnosticsBag = new System.Collections.Concurrent.ConcurrentBag<GDFileDiagnostics>();
            var analyzedFiles = new System.Collections.Concurrent.ConcurrentBag<string>();
            var timedOutFiles = new System.Collections.Concurrent.ConcurrentBag<string>();

            Parallel.ForEach(filesToAnalyze, options, filePath =>
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));

                try
                {
                    var script = GetScript(project, filePath);
                    if (script == null)
                        return;

                    var entry = AnalyzeFile(script, project);
                    _cache.Set(entry.Key, entry);

                    analyzedFiles.Add(filePath);
                    diagnosticsBag.Add(new GDFileDiagnostics
                    {
                        FilePath = filePath,
                        Items = entry.Diagnostics,
                        FromCache = false
                    });
                }
                catch (OperationCanceledException)
                {
                    timedOutFiles.Add(filePath);
                }
                catch (GDInvalidStateException ex)
                {
                    _logger?.Warning($"Analysis failed for {filePath}: {ex.Message}");
                }
                catch (IOException ex)
                {
                    _logger?.Warning($"IO error analyzing {filePath}: {ex.Message}");
                }
            });

            result.AnalyzedFiles.AddRange(analyzedFiles);
            result.TimedOutFiles.AddRange(timedOutFiles);
            result.Diagnostics.AddRange(diagnosticsBag);

            // Add cached results for unchanged files
            AddCachedResults(project, filesToAnalyze, result);
        }
        catch (Exception ex)
        {
            return GDIncrementalAnalysisResult.Failed(ex.Message);
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;

        return result;
    }

    /// <inheritdoc />
    public GDIncrementalState GetState()
    {
        return GDIncrementalState.Create(_projectPath, _tracker, _dependencies, ToolVersion);
    }

    /// <inheritdoc />
    public void Invalidate(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            _cache.RemoveByFilePath(path);
            _tracker.Remove(path);

            // Also invalidate dependents
            var dependents = _dependencies.GetTransitiveDependents(path);
            foreach (var dep in dependents)
            {
                _cache.RemoveByFilePath(dep);
                _tracker.Remove(dep);
            }
        }
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _cache.Clear();
        _tracker.Clear();
        _dependencies.Clear();
    }

    /// <inheritdoc />
    public async Task SaveStateAsync(string directory)
    {
        var state = GetState();
        await state.SaveAsync(directory);
    }

    /// <inheritdoc />
    public async Task<bool> LoadStateAsync(string directory)
    {
        var state = await GDIncrementalState.LoadAsync(directory);
        if (state == null)
            return false;

        // Verify tool version compatibility
        if (state.ToolVersion != ToolVersion)
        {
            // Version mismatch, start fresh
            ClearCache();
            return false;
        }

        state.ApplyTo(_tracker, _dependencies);
        return true;
    }

    private HashSet<string> GetFilesToAnalyze(GDFileChanges changes, GDScriptProject project)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Direct changes
        files.UnionWith(changes.Added);
        files.UnionWith(changes.Modified);

        // Cascade to dependents
        foreach (var modified in changes.Modified.Concat(changes.Deleted))
        {
            var dependents = _dependencies.GetTransitiveDependents(modified);
            files.UnionWith(dependents);
        }

        // Handle deleted file dependencies
        foreach (var deleted in changes.Deleted)
        {
            _dependencies.RemoveFile(deleted);
            _cache.RemoveByFilePath(deleted);
        }

        return files;
    }

    /// <summary>
    /// Represents file content with pre-computed hash.
    /// </summary>
    private record FileContent(string Content, string Hash);

    /// <summary>
    /// Reads file and computes hash once.
    /// </summary>
    private static FileContent ReadFileWithHash(string path)
    {
        var content = File.ReadAllText(path);
        var hash = GDCacheKey.ComputeContentHash(content);
        return new FileContent(content, hash);
    }

    private GDCachedAnalysisEntry AnalyzeFile(GDScriptFile script, GDScriptProject project)
    {
        // Read file and compute hash once
        var fileContent = ReadFileWithHash(script.FullPath!);
        var relativePath = Path.GetRelativePath(_projectPath, script.FullPath!).Replace('\\', '/');
        var key = GDCacheKey.CreateWithHash(relativePath, fileContent.Hash);

        var entry = new GDCachedAnalysisEntry
        {
            Key = key,
            FilePath = script.FullPath!,
            ContentHash = key.ContentHash,
            CachedAt = DateTime.UtcNow
        };

        // Run validation and linting on the parsed AST
        if (script.Class != null)
        {
            var diagnostics = new List<GDSerializedDiagnostic>();

            // Run validator
            try
            {
                var validationResult = _validator.Validate(script.Class, _validationOptions);
                foreach (var diagnostic in validationResult.Diagnostics)
                {
                    diagnostics.Add(SerializeValidatorDiagnostic(diagnostic));
                }
            }
            catch (GDInvalidStateException ex)
            {
                _logger?.Warning($"Validation failed for {script.FullPath}: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                _logger?.Warning($"Validation argument error for {script.FullPath}: {ex.Message}");
            }

            // Run linter
            try
            {
                var lintResult = _linter.Lint(script.Class);
                foreach (var issue in lintResult.Issues)
                {
                    diagnostics.Add(SerializeLintIssue(issue));
                }
            }
            catch (GDInvalidStateException ex)
            {
                _logger?.Warning($"Linting failed for {script.FullPath}: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                _logger?.Warning($"Linting argument error for {script.FullPath}: {ex.Message}");
            }

            entry.Diagnostics = diagnostics;
        }

        // Track dependencies (e.g., extends, preload)
        var deps = ExtractDependencies(script, project);
        entry.Dependencies = deps;
        _dependencies.SetDependencies(script.FullPath!, deps);

        return entry;
    }

    /// <summary>
    /// Converts a validator diagnostic to a serialized format for caching.
    /// </summary>
    private static GDSerializedDiagnostic SerializeValidatorDiagnostic(GDDiagnostic diagnostic)
    {
        return new GDSerializedDiagnostic
        {
            Code = diagnostic.CodeString,
            Message = diagnostic.Message,
            Severity = MapValidatorSeverity(diagnostic.Severity),
            StartLine = diagnostic.StartLine,
            StartColumn = diagnostic.StartColumn,
            EndLine = diagnostic.EndLine,
            EndColumn = diagnostic.EndColumn,
            Source = "validator"
        };
    }

    /// <summary>
    /// Converts a lint issue to a serialized format for caching.
    /// </summary>
    private static GDSerializedDiagnostic SerializeLintIssue(GDLintIssue issue)
    {
        return new GDSerializedDiagnostic
        {
            Code = issue.RuleId,
            Message = issue.Message,
            Severity = MapLintSeverity(issue.Severity),
            StartLine = issue.StartLine,
            StartColumn = issue.StartColumn,
            EndLine = issue.EndLine,
            EndColumn = issue.EndColumn,
            Source = "linter"
        };
    }

    /// <summary>
    /// Maps validator severity to the serialized severity (0=Error, 1=Warning, 2=Info, 3=Hint).
    /// </summary>
    private static int MapValidatorSeverity(Reader.GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            Reader.GDDiagnosticSeverity.Error => 0,
            Reader.GDDiagnosticSeverity.Warning => 1,
            Reader.GDDiagnosticSeverity.Hint => 3,
            _ => 2 // Info
        };
    }

    /// <summary>
    /// Maps linter severity to the serialized severity (0=Error, 1=Warning, 2=Info, 3=Hint).
    /// </summary>
    private static int MapLintSeverity(GDLintSeverity severity)
    {
        return severity switch
        {
            GDLintSeverity.Error => 0,
            GDLintSeverity.Warning => 1,
            GDLintSeverity.Info => 2,
            GDLintSeverity.Hint => 3,
            _ => 2 // Info
        };
    }

    private HashSet<string> ExtractDependencies(GDScriptFile script, GDScriptProject project)
    {
        var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (script.Class == null)
            return deps;

        // 1. Extract extends statement dependency
        var extendsAttr = script.Class.Extends;
        if (extendsAttr?.Type != null)
        {
            var extendsPath = extendsAttr.Type.ToString();
            var resolved = ResolveResourcePath(extendsPath, project);
            if (resolved != null)
                deps.Add(resolved);
        }

        // 2. Extract preload/load call dependencies
        foreach (var node in script.Class.AllNodes)
        {
            if (node is GDCallExpression call)
            {
                var callerName = GetCallerName(call);
                if (GDWellKnownFunctions.IsResourceLoader(callerName))
                {
                    var firstParam = call.Parameters?.FirstOrDefault();
                    if (firstParam is GDStringExpression strExpr && strExpr.String != null)
                    {
                        var resourcePath = strExpr.String.Sequence;
                        var resolved = ResolveResourcePath(resourcePath, project);
                        if (resolved != null)
                            deps.Add(resolved);
                    }
                }
            }
        }

        return deps;
    }

    private static string? GetCallerName(GDCallExpression call)
    {
        if (call.CallerExpression is GDIdentifierExpression idExpr)
            return idExpr.Identifier?.Sequence;
        return null;
    }

    private static string? ResolveResourcePath(string? path, GDScriptProject project)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // Handle res:// paths
        if (path.StartsWith("res://"))
        {
            var relativePath = path.Substring(6); // Remove "res://"
            return project.ScriptFiles?
                .FirstOrDefault(f => f.FullPath != null &&
                    f.FullPath.EndsWith(relativePath, StringComparison.OrdinalIgnoreCase))
                ?.FullPath;
        }

        // Handle quoted paths (extends "path.gd")
        var trimmedPath = path.Trim('"', '\'');
        if (trimmedPath.EndsWith(".gd", StringComparison.OrdinalIgnoreCase))
        {
            return project.ScriptFiles?
                .FirstOrDefault(f => f.FullPath != null &&
                    f.FullPath.EndsWith(trimmedPath, StringComparison.OrdinalIgnoreCase))
                ?.FullPath;
        }

        return null;
    }

    private void AddCachedResults(
        GDScriptProject project,
        HashSet<string> analyzedFiles,
        GDIncrementalAnalysisResult result)
    {
        if (project.ScriptFiles == null)
            return;

        foreach (var script in project.ScriptFiles)
        {
            if (script.FullPath == null || analyzedFiles.Contains(script.FullPath))
                continue;

            FileContent fileContent;
            try
            {
                fileContent = ReadFileWithHash(script.FullPath);
            }
            catch (IOException)
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(_projectPath, script.FullPath).Replace('\\', '/');
            var key = GDCacheKey.CreateWithHash(relativePath, fileContent.Hash);

            if (_cache.TryGet(key, out var cached) && cached != null)
            {
                result.CachedFiles.Add(script.FullPath);
                result.Diagnostics.Add(new GDFileDiagnostics
                {
                    FilePath = script.FullPath,
                    Items = cached.Diagnostics,
                    FromCache = true
                });
            }
        }
    }

    private static GDScriptFile? GetScript(GDScriptProject project, string filePath)
    {
        if (project.ScriptFiles == null)
            return null;

        return project.ScriptFiles.FirstOrDefault(s =>
            s.FullPath != null &&
            s.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
    }
}
