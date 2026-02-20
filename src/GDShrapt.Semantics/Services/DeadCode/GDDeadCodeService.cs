using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for detecting dead code in GDScript projects.
/// Uses semantic analysis with GDProjectSemanticModel for accurate detection.
/// </summary>
public class GDDeadCodeService
{
    private readonly GDProjectSemanticModel _projectModel;
    private readonly GDScriptProject _project;
    private readonly GDCallSiteRegistry? _callSiteRegistry;
    private readonly GDSignalConnectionRegistry _signalRegistry;
    private Dictionary<string, string>? _autoloadNamesByPath;


    /// <summary>
    /// Creates a dead code service using semantic analysis.
    /// </summary>
    /// <param name="projectModel">The project semantic model (required).</param>
    /// <exception cref="ArgumentNullException">Thrown if projectModel is null.</exception>
    internal GDDeadCodeService(GDProjectSemanticModel projectModel)
    {
        _projectModel = projectModel ?? throw new ArgumentNullException(nameof(projectModel));
        _project = projectModel.Project;
        _callSiteRegistry = _project.CallSiteRegistry;
        _signalRegistry = projectModel.SignalConnectionRegistry;
    }

    /// <summary>
    /// Gets the autoload name for a script file, if registered as an autoload.
    /// Returns null if the file is not an autoload.
    /// </summary>
    private string? GetAutoloadName(GDScriptFile file)
    {
        if (_autoloadNamesByPath == null)
        {
            _autoloadNamesByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _project.AutoloadEntries)
            {
                // Resolve res:// path to full path, normalize to forward slashes
                // to match GDScriptReference.NormalizePath convention
                var resPath = entry.Path;
                if (resPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                    resPath = resPath.Substring(6);

                var fullPath = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(_project.ProjectPath, resPath))
                    .Replace('\\', '/').TrimEnd('/');
                _autoloadNamesByPath[fullPath] = entry.Name;
            }
        }

        if (file.FullPath != null && _autoloadNamesByPath.TryGetValue(file.FullPath, out var name))
            return name;

        return null;
    }

    /// <summary>
    /// Gets all names under which a file's members can be accessed cross-file.
    /// Includes the TypeName (class_name or filename) and the autoload name if different.
    /// </summary>
    private List<string> GetEffectiveClassNames(GDScriptFile file, string? className)
    {
        var names = new List<string>();
        if (!string.IsNullOrEmpty(className))
            names.Add(className);

        var autoloadName = GetAutoloadName(file);
        if (!string.IsNullOrEmpty(autoloadName) && !names.Contains(autoloadName, StringComparer.OrdinalIgnoreCase))
            names.Add(autoloadName);

        return names;
    }

    /// <summary>
    /// Analyzes the entire project for dead code using semantic analysis.
    /// </summary>
    public GDDeadCodeReport AnalyzeProject(GDDeadCodeOptions options)
    {
        var items = new List<GDDeadCodeItem>();
        int filesAnalyzed = 0;

        foreach (var file in _project.ScriptFiles)
        {
            filesAnalyzed++;
            items.AddRange(AnalyzeFileInternal(file, options));
        }

        var report = new GDDeadCodeReport(items);
        report.FilesAnalyzed = filesAnalyzed;
        report.SceneSignalConnectionsConsidered = _signalRegistry.GetAllConnections()
            .Count(c => c.IsSceneConnection);
        report.VirtualMethodsSkipped = options.SkipMethods.Count;
        report.AutoloadsResolved = _autoloadNamesByPath?.Count ?? 0;
        report.TotalCallSitesRegistered = _callSiteRegistry?.GetAllTargets().Count() ?? 0;
        return report;
    }

    /// <summary>
    /// Analyzes a single file for dead code using semantic analysis.
    /// </summary>
    public GDDeadCodeReport AnalyzeFile(GDScriptFile file, GDDeadCodeOptions options)
    {
        var items = AnalyzeFileInternal(file, options).ToList();
        return new GDDeadCodeReport(items);
    }

    private IEnumerable<GDDeadCodeItem> AnalyzeFileInternal(GDScriptFile file, GDDeadCodeOptions options)
    {
        if (file?.Class == null)
            yield break;

        if (options.ShouldSkipFile(file.FullPath ?? ""))
            yield break;

        var classDecl = file.Class;
        var className = file.TypeName ?? classDecl.ClassName?.Identifier?.Sequence;
        var effectiveNames = GetEffectiveClassNames(file, className);

        // Get semantic model for this file
        var semanticModel = _projectModel.GetSemanticModel(file);
        if (semanticModel == null)
        {
            throw new InvalidOperationException(
                $"Semantic model is required but unavailable for file: {file.FullPath}. " +
                "Ensure the project is loaded with semantic analysis enabled.");
        }

        // 1. Find unused variables
        if (options.IncludeVariables)
        {
            foreach (var item in FindUnusedVariables(file, classDecl, effectiveNames, semanticModel, options))
                yield return item;
        }

        // 2. Find unused functions
        if (options.IncludeFunctions)
        {
            foreach (var item in FindUnusedFunctions(file, classDecl, effectiveNames, semanticModel, options))
                yield return item;
        }

        // 3. Find unused signals
        if (options.IncludeSignals)
        {
            foreach (var item in FindUnusedSignals(file, classDecl, effectiveNames, semanticModel, options))
                yield return item;
        }

        // 4. Find unused parameters
        if (options.IncludeParameters)
        {
            foreach (var item in FindUnusedParameters(file, classDecl, semanticModel, options))
                yield return item;
        }

        // 5. Find unreachable code
        if (options.IncludeUnreachable)
        {
            foreach (var item in FindUnreachableCode(file, classDecl, options))
                yield return item;
        }

        // 6. Find unused constants
        if (options.IncludeConstants)
        {
            foreach (var item in FindUnusedConstants(file, classDecl, effectiveNames, semanticModel, options))
                yield return item;
        }

        // 7. Find unused enum values
        if (options.IncludeEnumValues)
        {
            foreach (var item in FindUnusedEnumValues(file, classDecl, semanticModel, options))
                yield return item;
        }

        // 8. Find unused inner classes
        if (options.IncludeInnerClasses)
        {
            foreach (var item in FindUnusedInnerClasses(file, classDecl, semanticModel, options))
                yield return item;
        }
    }

    /// <summary>
    /// Finds unused class-level variables using semantic reference tracking.
    /// Properly handles shadowing by using scope-aware symbol resolution.
    /// </summary>
    private IEnumerable<GDDeadCodeItem> FindUnusedVariables(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        List<string> effectiveNames,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        // Get all class-level variables (excluding constants - handled separately)
        var variables = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .Where(v => !v.IsConstant)
            .ToList();

        foreach (var variable in variables)
        {
            var varName = variable.Identifier?.Sequence;
            if (string.IsNullOrEmpty(varName))
                continue;

            bool isPrivate = varName.StartsWith("_");

            // Skip private if not included
            if (isPrivate && !options.IncludePrivate)
                continue;

            // Find the symbol for this variable
            var symbol = semanticModel.FindSymbol(varName);
            if (symbol == null)
                continue;

            // Get all references to this symbol
            var references = semanticModel.GetReferencesTo(symbol);

            // Filter to only reads (not writes/assignments)
            // A variable that is only written to is still dead code
            var reads = references.Where(r => r.IsRead).ToList();

            if (reads.Count == 0)
            {
                // Check cross-file access using all effective names
                // (includes both TypeName and autoload name)
                if (HasCrossFileMemberAccess(file, effectiveNames, varName))
                    continue;

                // Check for @export/@onready annotations → downgrade to Potential
                bool isExport = false;
                bool isOnready = false;
                foreach (var attr in variable.AttributesDeclaredBefore)
                {
                    var attrName = attr.Attribute?.Name?.Sequence;
                    if (attrName == null) continue;
                    if (attrName == "export" || attrName.StartsWith("export_"))
                        isExport = true;
                    else if (attrName == "onready")
                        isOnready = true;
                }

                bool hasExportOrOnready = isExport || isOnready;
                var confidence = hasExportOrOnready
                    ? GDReferenceConfidence.Potential
                    : GDReferenceConfidence.Strict;
                var reasonCode = isExport ? GDDeadCodeReasonCode.VEX
                    : isOnready ? GDDeadCodeReasonCode.VOR
                    : GDDeadCodeReasonCode.VNR;

                var token = variable.Identifier ?? variable.AllTokens.FirstOrDefault();
                yield return new GDDeadCodeItem(GDDeadCodeKind.Variable, varName, file.FullPath ?? "")
                {
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0,
                    EndLine = token?.EndLine ?? 0,
                    EndColumn = token?.EndColumn ?? 0,
                    Confidence = confidence,
                    ReasonCode = reasonCode,
                    Reason = "Variable is never read (semantic analysis)",
                    IsPrivate = isPrivate,
                    IsExportedOrOnready = hasExportOrOnready
                };
            }
        }
    }

    /// <summary>
    /// Finds unused functions using semantic call site registry and reference tracking.
    /// </summary>
    private IEnumerable<GDDeadCodeItem> FindUnusedFunctions(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        List<string> effectiveNames,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        var methods = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .ToList();

        foreach (var method in methods)
        {
            var methodName = method.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                continue;

            // Skip Godot virtual methods (configurable via options)
            if (options.ShouldSkipMethod(methodName))
                continue;

            bool isPrivate = methodName.StartsWith("_") && !options.ShouldSkipMethod(methodName);

            // Skip private if not included
            if (isPrivate && !options.IncludePrivate)
                continue;

            // Check if method has any callers
            var (hasCallers, confidence, reason) = CheckMethodCallers(
                file, effectiveNames, methodName, semanticModel, options);

            if (!hasCallers)
            {
                var reasonCode = confidence == GDReferenceConfidence.Strict
                    ? GDDeadCodeReasonCode.FNC
                    : GDDeadCodeReasonCode.FDT;

                // Use Identifier for accurate line position
                var identToken = method.Identifier ?? method.FuncKeyword ?? method.AllTokens.FirstOrDefault();
                var endToken = method.AllTokens.LastOrDefault();

                var item = new GDDeadCodeItem(GDDeadCodeKind.Function, methodName, file.FullPath ?? "")
                {
                    Line = identToken?.StartLine ?? 0,
                    Column = identToken?.StartColumn ?? 0,
                    EndLine = endToken?.EndLine ?? 0,
                    EndColumn = endToken?.EndColumn ?? 0,
                    Confidence = confidence,
                    ReasonCode = reasonCode,
                    Reason = reason,
                    IsPrivate = isPrivate
                };

                if (options.CollectEvidence)
                {
                    item.Evidence = new GDDeadCodeEvidence
                    {
                        CallSitesScanned = _callSiteRegistry?.GetAllTargets().Count() ?? 0,
                        CrossFileAccessChecks = effectiveNames.Count,
                        IsVirtualOrEntrypoint = options.ShouldSkipMethod(methodName)
                    };
                }

                yield return item;
            }
        }
    }

    /// <summary>
    /// Checks if any other file in the project has member accesses matching any of the effective names.
    /// </summary>
    private bool HasCrossFileMemberAccess(GDScriptFile file, List<string> effectiveNames, string memberName)
    {
        if (effectiveNames.Count == 0)
            return false;

        foreach (var otherFile in _project.ScriptFiles)
        {
            if (otherFile == file || otherFile.Class == null)
                continue;

            var otherModel = _projectModel.GetSemanticModel(otherFile);
            if (otherModel == null)
                continue;

            foreach (var name in effectiveNames)
            {
                if (otherModel.HasMemberAccesses(name, memberName))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a method has any callers using semantic registries.
    /// Returns (hasCallers, confidence, reason).
    /// </summary>
    private (bool HasCallers, GDReferenceConfidence Confidence, string Reason) CheckMethodCallers(
        GDScriptFile file,
        List<string> effectiveNames,
        string methodName,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        // 1. Check cross-file calls via CallSiteRegistry (check all effective names)
        if (_callSiteRegistry != null)
        {
            foreach (var name in effectiveNames)
            {
                var callers = _callSiteRegistry.GetCallersOf(name, methodName);
                if (callers.Count > 0)
                    return (true, GDReferenceConfidence.Strict, "Has callers");
            }
        }

        // 2. Check signal connections (check all effective names)
        foreach (var name in effectiveNames)
        {
            var signalConnections = _signalRegistry.GetSignalsCallingMethod(name, methodName);
            if (signalConnections.Count > 0)
                return (true, GDReferenceConfidence.Strict, "Connected to signals");
        }

        // 3. Check local references within the file via semantic model
        var symbol = semanticModel.FindSymbol(methodName);
        if (symbol != null)
        {
            var refs = semanticModel.GetReferencesTo(symbol);
            if (refs.Count > 0)
            {
                return (true, GDReferenceConfidence.Strict, "Has local references");
            }
        }

        // 3.5 Check cross-file member access (e.g., GameManager.start_game() from another file)
        if (HasCrossFileMemberAccess(file, effectiveNames, methodName))
        {
            return (true, GDReferenceConfidence.Strict, "Has cross-file callers");
        }

        // 3.6 Check if this is an override of a base class method that has callers
        if (IsOverrideMethodUsed(file, methodName))
            return (true, GDReferenceConfidence.Strict, "Override of called base method");

        // 3.7 Check duck-typed ("*") call site entries — calls on unresolved receiver types
        if (_callSiteRegistry != null)
        {
            var duckCallers = _callSiteRegistry.GetCallersOf("*", methodName);
            if (duckCallers.Count > 0)
            {
                return (false, GDReferenceConfidence.Potential,
                    $"May be called via duck-typing ({duckCallers.Count} potential site(s))");
            }
        }

        // 4. Check duck-type calls (by method name only) if allowed
        if (options.MaxConfidence >= GDReferenceConfidence.NameMatch && _callSiteRegistry != null)
        {
            var allTargets = _callSiteRegistry.GetAllTargets();
            var duckTypeCallsCount = allTargets.Count(t =>
                string.Equals(t.MethodName, methodName, StringComparison.OrdinalIgnoreCase));

            if (duckTypeCallsCount > 0)
            {
                return (false, GDReferenceConfidence.NameMatch,
                    $"May be called via duck-typing ({duckTypeCallsCount} potential sites)");
            }
        }

        return (false, GDReferenceConfidence.Strict, "No callers found");
    }

    /// <summary>
    /// Checks if a method is an override of a base class method that has callers.
    /// If ParentClass.M() is called anywhere, ChildClass.M() (an override) is also used.
    /// </summary>
    private bool IsOverrideMethodUsed(GDScriptFile file, string methodName)
    {
        if (file.Class == null)
            return false;

        var extendsTypeName = file.Class.Extends?.Type?.BuildName();
        if (string.IsNullOrEmpty(extendsTypeName))
            return false;

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = extendsTypeName;

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            var parentFile = _project.GetScriptByTypeName(current);
            if (parentFile?.Class == null)
                break;

            var hasMethod = parentFile.Class.Members
                .OfType<GDMethodDeclaration>()
                .Any(m => m.Identifier?.Sequence == methodName);

            if (hasMethod)
            {
                var parentEffectiveNames = GetEffectiveClassNames(parentFile, parentFile.TypeName);

                if (_callSiteRegistry != null)
                {
                    foreach (var name in parentEffectiveNames)
                    {
                        if (_callSiteRegistry.GetCallersOf(name, methodName).Count > 0)
                            return true;
                    }
                }

                if (HasCrossFileMemberAccess(parentFile, parentEffectiveNames, methodName))
                    return true;

                var parentModel = _projectModel.GetSemanticModel(parentFile);
                if (parentModel != null)
                {
                    var sym = parentModel.FindSymbol(methodName);
                    if (sym != null && parentModel.GetReferencesTo(sym).Count > 0)
                        return true;
                }
            }

            current = parentFile.Class.Extends?.Type?.BuildName();
        }

        return false;
    }

    /// <summary>
    /// Finds unused signals using semantic signal registry and emit tracking.
    /// </summary>
    private IEnumerable<GDDeadCodeItem> FindUnusedSignals(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        List<string> effectiveNames,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        var signals = classDecl.Members
            .OfType<GDSignalDeclaration>()
            .ToList();

        foreach (var signal in signals)
        {
            var signalName = signal.Identifier?.Sequence;
            if (string.IsNullOrEmpty(signalName))
                continue;

            bool isPrivate = signalName.StartsWith("_");

            if (isPrivate && !options.IncludePrivate)
                continue;

            // Check if signal is emitted using semantic model
            var signalSymbol = semanticModel.FindSymbol(signalName);
            var signalRefs = signalSymbol != null
                ? semanticModel.GetReferencesTo(signalSymbol)
                : Array.Empty<GDReference>();

            // Check for emit calls - references where signal is accessed for .emit()
            bool isEmitted = signalRefs.Any(r => IsSignalEmitReference(r));

            // Check if signal has connections via registry (check all effective names)
            bool hasConnections = false;
            foreach (var name in effectiveNames)
            {
                var connections = _signalRegistry.GetCallbacksForSignal(name, signalName);
                if (connections.Count > 0)
                {
                    hasConnections = true;
                    break;
                }
            }

            // Broader connection check: match by signal name with any emitter type
            if (!hasConnections)
            {
                var allConns = _signalRegistry.GetAllConnections();
                hasConnections = allConns.Any(c => c.SignalName == signalName &&
                    (effectiveNames.Any(n => c.EmitterType == n) || c.EmitterType == null));
            }

            // Cross-file signal usage detection
            if (!isEmitted && !hasConnections)
            {
                isEmitted = HasCrossFileMemberAccess(file, effectiveNames, signalName);
            }

            if (!isEmitted && !hasConnections)
            {
                var token = signal.Identifier ?? signal.AllTokens.FirstOrDefault();

                yield return new GDDeadCodeItem(GDDeadCodeKind.Signal, signalName, file.FullPath ?? "")
                {
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0,
                    EndLine = token?.EndLine ?? 0,
                    EndColumn = token?.EndColumn ?? 0,
                    Confidence = GDReferenceConfidence.Strict,
                    ReasonCode = GDDeadCodeReasonCode.SNE,
                    Reason = "Signal is never emitted and has no connections",
                    IsPrivate = isPrivate
                };
            }
            else if (!isEmitted)
            {
                // Signal has connections but is never emitted - potentially dead
                var token = signal.Identifier ?? signal.AllTokens.FirstOrDefault();

                yield return new GDDeadCodeItem(GDDeadCodeKind.Signal, signalName, file.FullPath ?? "")
                {
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0,
                    EndLine = token?.EndLine ?? 0,
                    EndColumn = token?.EndColumn ?? 0,
                    Confidence = GDReferenceConfidence.Potential,
                    ReasonCode = GDDeadCodeReasonCode.SCB,
                    Reason = "Signal is connected but never emitted",
                    IsPrivate = isPrivate
                };
            }
        }
    }

    /// <summary>
    /// Checks if a reference is a signal emit (signal.emit() or emit_signal("name")).
    /// </summary>
    private static bool IsSignalEmitReference(GDReference reference)
    {
        var node = reference.ReferenceNode;
        if (node == null)
            return false;

        // Check for signal.emit() pattern
        var parent = node.Parent;
        if (parent is GDMemberOperatorExpression memberOp &&
            memberOp.Identifier?.Sequence == "emit")
        {
            var grandParent = memberOp.Parent;
            if (grandParent is GDCallExpression)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Finds unused parameters using semantic scope-aware reference tracking.
    /// </summary>
    private IEnumerable<GDDeadCodeItem> FindUnusedParameters(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        var methods = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .ToList();

        foreach (var method in methods)
        {
            var methodName = method.Identifier?.Sequence;
            if (string.IsNullOrEmpty(methodName))
                continue;

            // Skip Godot virtual methods - their signatures are fixed (configurable via options)
            if (options.ShouldSkipMethod(methodName))
                continue;

            var parameters = method.Parameters;
            if (parameters == null || parameters.Count == 0)
                continue;

            foreach (var param in parameters)
            {
                var paramName = param.Identifier?.Sequence;
                if (string.IsNullOrEmpty(paramName))
                    continue;

                // Skip if parameter starts with _ (intentionally unused convention)
                if (paramName.StartsWith("_"))
                    continue;

                // Use scope-aware lookup to find the parameter symbol
                // This ensures we find the parameter, not a same-named class member
                var paramSymbol = semanticModel.FindSymbolInScope(paramName, param);

                if (paramSymbol == null)
                    continue;

                // Get references to this parameter within the method
                var refs = semanticModel.GetReferencesTo(paramSymbol);

                // If no references (other than possible declaration), parameter is unused
                if (refs.Count == 0)
                {
                    var token = param.Identifier ?? param.AllTokens.FirstOrDefault();

                    yield return new GDDeadCodeItem(GDDeadCodeKind.Parameter, paramName, file.FullPath ?? "")
                    {
                        Line = token?.StartLine ?? 0,
                        Column = token?.StartColumn ?? 0,
                        EndLine = token?.EndLine ?? 0,
                        EndColumn = token?.EndColumn ?? 0,
                        Confidence = GDReferenceConfidence.Strict,
                        ReasonCode = GDDeadCodeReasonCode.PNU,
                        Reason = $"Parameter is never used in method '{methodName}'",
                        IsPrivate = false
                    };
                }
            }
        }
    }

    /// <summary>
    /// Finds unreachable code after return/break/continue statements.
    /// This uses simple control flow analysis as semantic model doesn't track reachability.
    /// </summary>
    private IEnumerable<GDDeadCodeItem> FindUnreachableCode(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        GDDeadCodeOptions options)
    {
        var methods = classDecl.Members
            .OfType<GDMethodDeclaration>()
            .ToList();

        foreach (var method in methods)
        {
            var methodName = method.Identifier?.Sequence ?? "";
            var statements = method.Statements;
            if (statements == null)
                continue;

            // Find unreachable statements after return/break/continue
            foreach (var unreachable in FindUnreachableStatements(statements))
            {
                var token = unreachable.AllTokens.FirstOrDefault();

                yield return new GDDeadCodeItem(GDDeadCodeKind.Unreachable, methodName, file.FullPath ?? "")
                {
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0,
                    EndLine = token?.EndLine ?? 0,
                    EndColumn = token?.EndColumn ?? 0,
                    Confidence = GDReferenceConfidence.Strict,
                    ReasonCode = GDDeadCodeReasonCode.UCR,
                    Reason = "Code is unreachable after return/break/continue",
                    IsPrivate = false
                };
            }
        }
    }

    /// <summary>
    /// Finds statements that are unreachable due to preceding return/break/continue.
    /// </summary>
    private static IEnumerable<GDNode> FindUnreachableStatements(GDStatementsList statements)
    {
        bool terminatorEncountered = false;

        foreach (var stmt in statements)
        {
            if (terminatorEncountered)
            {
                yield return stmt;
                continue;
            }

            // Check if this statement is a terminator
            if (stmt is GDExpressionStatement exprStmt)
            {
                if (exprStmt.Expression is GDReturnExpression ||
                    exprStmt.Expression is GDBreakExpression ||
                    exprStmt.Expression is GDContinueExpression)
                {
                    terminatorEncountered = true;
                }
            }
            else if (stmt is GDReturnExpression ||
                     stmt is GDBreakExpression ||
                     stmt is GDContinueExpression)
            {
                terminatorEncountered = true;
            }
        }
    }

    /// <summary>
    /// Finds unused constants (const declarations) using semantic reference tracking.
    /// </summary>
    private IEnumerable<GDDeadCodeItem> FindUnusedConstants(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        List<string> effectiveNames,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        // Get all constant declarations (variables with const keyword)
        var constants = classDecl.Members
            .OfType<GDVariableDeclaration>()
            .Where(v => v.IsConstant)
            .ToList();

        foreach (var constant in constants)
        {
            var constName = constant.Identifier?.Sequence;
            if (string.IsNullOrEmpty(constName))
                continue;

            bool isPrivate = constName.StartsWith("_");

            if (isPrivate && !options.IncludePrivate)
                continue;

            var symbol = semanticModel.FindSymbol(constName);
            if (symbol == null)
                continue;

            var references = semanticModel.GetReferencesTo(symbol);

            // Constants need at least one read reference to be considered used
            var reads = references.Where(r => r.IsRead).ToList();

            if (reads.Count == 0)
            {
                // Check cross-file access using all effective names
                if (HasCrossFileMemberAccess(file, effectiveNames, constName))
                    continue;

                var token = constant.Identifier ?? constant.AllTokens.FirstOrDefault();

                yield return new GDDeadCodeItem(GDDeadCodeKind.Constant, constName, file.FullPath ?? "")
                {
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0,
                    EndLine = token?.EndLine ?? 0,
                    EndColumn = token?.EndColumn ?? 0,
                    Confidence = GDReferenceConfidence.Strict,
                    ReasonCode = GDDeadCodeReasonCode.CNU,
                    Reason = "Constant is never used",
                    IsPrivate = isPrivate
                };
            }
        }
    }

    /// <summary>
    /// Finds unused enum values using semantic reference tracking.
    /// </summary>
    private IEnumerable<GDDeadCodeItem> FindUnusedEnumValues(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        var enums = classDecl.Members
            .OfType<GDEnumDeclaration>()
            .ToList();

        foreach (var enumDecl in enums)
        {
            var enumName = enumDecl.Identifier?.Sequence;
            if (enumDecl.Values == null)
                continue;

            foreach (var enumValue in enumDecl.Values.OfType<GDEnumValueDeclaration>())
            {
                var valueName = enumValue.Identifier?.Sequence;
                if (string.IsNullOrEmpty(valueName))
                    continue;

                // For enum values, we search for both EnumName.VALUE and just VALUE references
                var qualifiedName = !string.IsNullOrEmpty(enumName) ? $"{enumName}.{valueName}" : valueName;

                // Check if value is referenced
                var symbol = semanticModel.FindSymbol(valueName);
                var isUsed = false;

                if (symbol != null)
                {
                    var refs = semanticModel.GetReferencesTo(symbol);
                    isUsed = refs.Count > 0;
                }

                // Also check via qualified name pattern in the file content
                if (!isUsed && !string.IsNullOrEmpty(enumName))
                {
                    var qualifiedSymbol = semanticModel.FindSymbol(qualifiedName);
                    if (qualifiedSymbol != null)
                    {
                        var qualifiedRefs = semanticModel.GetReferencesTo(qualifiedSymbol);
                        isUsed = qualifiedRefs.Count > 0;
                    }
                }

                if (!isUsed)
                {
                    var token = enumValue.Identifier ?? enumValue.AllTokens.FirstOrDefault();

                    yield return new GDDeadCodeItem(GDDeadCodeKind.EnumValue, valueName, file.FullPath ?? "")
                    {
                        Line = token?.StartLine ?? 0,
                        Column = token?.StartColumn ?? 0,
                        EndLine = token?.EndLine ?? 0,
                        EndColumn = token?.EndColumn ?? 0,
                        Confidence = GDReferenceConfidence.Potential, // Enum values may be used via reflection
                        ReasonCode = GDDeadCodeReasonCode.ENU,
                        Reason = $"Enum value '{valueName}' in '{enumName ?? "anonymous"}' is never referenced",
                        IsPrivate = false
                    };
                }
            }
        }
    }

    /// <summary>
    /// Finds unused inner classes using semantic reference tracking.
    /// Checks for instantiation (new InnerClass()) and type references.
    /// </summary>
    private IEnumerable<GDDeadCodeItem> FindUnusedInnerClasses(
        GDScriptFile file,
        GDClassDeclaration classDecl,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        var innerClasses = classDecl.Members
            .OfType<GDInnerClassDeclaration>()
            .ToList();

        foreach (var innerClass in innerClasses)
        {
            var className = innerClass.Identifier?.Sequence;
            if (string.IsNullOrEmpty(className))
                continue;

            bool isPrivate = className.StartsWith("_");

            if (isPrivate && !options.IncludePrivate)
                continue;

            // Check if the inner class is referenced anywhere
            var symbol = semanticModel.FindSymbol(className);
            var isUsed = false;

            if (symbol != null)
            {
                var refs = semanticModel.GetReferencesTo(symbol);
                isUsed = refs.Count > 0;
            }

            // Also check if it's instantiated via .new() calls
            if (!isUsed)
            {
                // Check for ClassName.new() pattern in call sites
                if (_callSiteRegistry != null)
                {
                    var callers = _callSiteRegistry.GetCallersOf(className, "new");
                    isUsed = callers.Count > 0;
                }
            }

            if (!isUsed)
            {
                var token = innerClass.Identifier ?? innerClass.ClassKeyword ?? innerClass.AllTokens.FirstOrDefault();
                var endToken = innerClass.AllTokens.LastOrDefault();

                yield return new GDDeadCodeItem(GDDeadCodeKind.InnerClass, className, file.FullPath ?? "")
                {
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0,
                    EndLine = endToken?.EndLine ?? 0,
                    EndColumn = endToken?.EndColumn ?? 0,
                    Confidence = GDReferenceConfidence.Potential, // Inner classes may be used externally
                    ReasonCode = GDDeadCodeReasonCode.ICU,
                    Reason = "Inner class is never instantiated or referenced",
                    IsPrivate = isPrivate
                };
            }
        }
    }
}
