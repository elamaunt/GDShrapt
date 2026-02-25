using GDShrapt.Abstractions;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Semantics;

/// <summary>
/// Service for detecting dead code in GDScript projects.
/// Consumes only semantic API — no direct AST access.
/// </summary>
public class GDDeadCodeService
{
    private readonly GDProjectSemanticModel _projectModel;
    private readonly GDScriptProject _project;
    private readonly GDCallSiteRegistry? _callSiteRegistry;
    private readonly GDSignalConnectionRegistry _signalRegistry;
    private Dictionary<string, string>? _autoloadNamesByPath;
    private int _annotationSuppressedCount;


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
        var droppedByReflection = options.CollectDroppedByReflection
            ? new List<GDReflectionDroppedItem>()
            : null;
        int filesAnalyzed = 0;
        _annotationSuppressedCount = 0;

        foreach (var file in _project.ScriptFiles)
        {
            filesAnalyzed++;
            items.AddRange(AnalyzeFileInternal(file, options, droppedByReflection));
        }

        var report = new GDDeadCodeReport(items);
        report.FilesAnalyzed = filesAnalyzed;
        report.AnnotationSuppressedCount = _annotationSuppressedCount;
        report.SceneSignalConnectionsConsidered = _signalRegistry.GetAllConnections()
            .Count(c => c.IsSceneConnection);
        report.VirtualMethodsSkipped = options.SkipMethods.Count;
        report.AutoloadsResolved = _autoloadNamesByPath?.Count ?? 0;
        report.TotalCallSitesRegistered = _callSiteRegistry?.GetAllTargets().Count() ?? 0;
        report.CSharpCodeDetected = _projectModel.CSharpInterop.HasCSharpCode;
        report.CSharpInteropExcluded = items.Count(i => i.ReasonCode == GDDeadCodeReasonCode.CSI);
        report.ResourceFilesConsidered = _project.TresResourceProvider?.ResourceCount ?? 0;
        if (droppedByReflection != null)
            report.DroppedByReflection = droppedByReflection;
        return report;
    }

    /// <summary>
    /// Analyzes a single file for dead code using semantic analysis.
    /// </summary>
    public GDDeadCodeReport AnalyzeFile(GDScriptFile file, GDDeadCodeOptions options)
    {
        var droppedByReflection = options.CollectDroppedByReflection
            ? new List<GDReflectionDroppedItem>()
            : null;
        _annotationSuppressedCount = 0;
        var items = AnalyzeFileInternal(file, options, droppedByReflection).ToList();
        var report = new GDDeadCodeReport(items);
        report.AnnotationSuppressedCount = _annotationSuppressedCount;
        if (droppedByReflection != null)
            report.DroppedByReflection = droppedByReflection;
        return report;
    }

    private IEnumerable<GDDeadCodeItem> AnalyzeFileInternal(
        GDScriptFile file,
        GDDeadCodeOptions options,
        List<GDReflectionDroppedItem>? droppedByReflection)
    {
        if (file?.Class == null)
            yield break;

        if (options.ShouldSkipFile(file.FullPath ?? ""))
            yield break;

        var className = file.TypeName;
        var effectiveNames = GetEffectiveClassNames(file, className);

        bool isAutoload = GetAutoloadName(file) != null;
        bool csharpReachable = isAutoload && _projectModel.CSharpInterop.HasCSharpCode;

        var semanticModel = _projectModel.GetSemanticModel(file);
        if (semanticModel == null)
        {
            throw new InvalidOperationException(
                $"Semantic model is required but unavailable for file: {file.FullPath}. " +
                "Ensure the project is loaded with semantic analysis enabled.");
        }

        if (options.IncludeVariables)
        {
            foreach (var item in FindUnusedVariables(file, effectiveNames, semanticModel, options, droppedByReflection, csharpReachable))
                yield return item;
        }

        if (options.IncludeFunctions)
        {
            foreach (var item in FindUnusedFunctions(file, effectiveNames, semanticModel, options, droppedByReflection, csharpReachable))
                yield return item;
        }

        if (options.IncludeSignals)
        {
            foreach (var item in FindUnusedSignals(file, effectiveNames, semanticModel, options, droppedByReflection, csharpReachable))
                yield return item;
        }

        if (options.IncludeParameters)
        {
            foreach (var item in FindUnusedParameters(file, semanticModel, options))
                yield return item;
        }

        if (options.IncludeUnreachable)
        {
            foreach (var item in FindUnreachableCode(file, semanticModel, options))
                yield return item;
        }

        if (options.IncludeConstants)
        {
            foreach (var item in FindUnusedConstants(file, effectiveNames, semanticModel, options, csharpReachable))
                yield return item;
        }

        if (options.IncludeEnumValues)
        {
            foreach (var item in FindUnusedEnumValues(file, semanticModel, options))
                yield return item;
        }

        if (options.IncludeInnerClasses)
        {
            foreach (var item in FindUnusedInnerClasses(file, semanticModel, options))
                yield return item;
        }
    }

    private IEnumerable<GDDeadCodeItem> FindUnusedVariables(
        GDScriptFile file,
        List<string> effectiveNames,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options,
        List<GDReflectionDroppedItem>? droppedByReflection,
        bool csharpReachable = false)
    {
        foreach (var symbol in semanticModel.GetVariables())
        {
            var varName = symbol.Name;
            if (string.IsNullOrEmpty(varName))
                continue;

            // Constants are handled separately
            if (symbol.Kind == GDSymbolKind.Constant)
                continue;

            if (options.RespectSuppressionAnnotations && IsAnnotationSuppressed(semanticModel, varName, options))
            {
                _annotationSuppressedCount++;
                continue;
            }

            bool isPrivate = varName.StartsWith("_");

            if (isPrivate && !options.IncludePrivate)
                continue;

            var references = semanticModel.GetReferencesTo(symbol);
            var reads = references.Where(r => r.IsRead).ToList();

            if (reads.Count == 0)
            {
                // Check same-file duck-typed member access (RC5: self.prop, autoload.SubSystem.prop)
                if (symbol.DeclaringScopeNode == null && semanticModel.HasMemberAccessesIncludingDuckTyped(
                        file.TypeName ?? "", varName))
                    continue;

                if (HasCrossFileMemberAccess(file, effectiveNames, varName))
                    continue;

                var (propReflReachable, propReflSite) = IsReachableViaReflection(
                    file, effectiveNames, varName, GDReflectionKind.Property);
                if (propReflReachable)
                {
                    if (droppedByReflection != null && propReflSite != null)
                    {
                        var posToken = symbol.PositionToken;
                        droppedByReflection.Add(new GDReflectionDroppedItem
                        {
                            Kind = GDDeadCodeKind.Variable,
                            Name = varName,
                            FilePath = file.FullPath ?? "",
                            Line = posToken?.StartLine ?? 0,
                            Column = posToken?.StartColumn ?? 0,
                            ReflectionKind = propReflSite.Kind,
                            ReflectionSiteFile = propReflSite.FilePath,
                            ReflectionSiteLine = propReflSite.Line,
                            CallMethod = propReflSite.CallMethod,
                            ReceiverTypeName = propReflSite.ReceiverTypeName,
                            NameFilters = propReflSite.NameFilters
                        });
                    }
                    continue;
                }

                bool isExport = semanticModel.IsExportVariable(varName);
                bool isOnready = semanticModel.IsOnreadyVariable(varName);
                bool hasExportOrOnready = isExport || isOnready;
                var confidence = hasExportOrOnready
                    ? GDReferenceConfidence.Potential
                    : GDReferenceConfidence.Strict;
                var reasonCode = isExport ? GDDeadCodeReasonCode.VEX
                    : isOnready ? GDDeadCodeReasonCode.VOR
                    : GDDeadCodeReasonCode.VNR;

                if (file.IsGlobal && !isPrivate && options.TreatClassNameAsPublicAPI
                    && confidence == GDReferenceConfidence.Strict)
                {
                    confidence = GDReferenceConfidence.Potential;
                    reasonCode = GDDeadCodeReasonCode.FPA;
                }

                if (csharpReachable && !isPrivate && confidence == GDReferenceConfidence.Strict)
                {
                    confidence = GDReferenceConfidence.Potential;
                    reasonCode = GDDeadCodeReasonCode.CSI;
                }

                if (!isPrivate && confidence == GDReferenceConfidence.Strict
                    && semanticModel.IsSelfPassedExternally())
                {
                    confidence = GDReferenceConfidence.Potential;
                    reasonCode = GDDeadCodeReasonCode.VDA;
                }

                var token = symbol.PositionToken;
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
            else if (reads.All(r => r.IsPropertyWriteOnCaller)
                     && IsLocallyConstructedNonEscaping(symbol, references))
            {
                var token = symbol.PositionToken;
                yield return new GDDeadCodeItem(GDDeadCodeKind.Variable, varName, file.FullPath ?? "")
                {
                    Line = token?.StartLine ?? 0,
                    Column = token?.StartColumn ?? 0,
                    EndLine = token?.EndLine ?? 0,
                    EndColumn = token?.EndColumn ?? 0,
                    Confidence = GDReferenceConfidence.Strict,
                    ReasonCode = GDDeadCodeReasonCode.VPW,
                    Reason = "Variable only used as property/indexer write target on locally-constructed non-escaping object",
                    IsPrivate = isPrivate
                };
            }
        }
    }

    private IEnumerable<GDDeadCodeItem> FindUnusedFunctions(
        GDScriptFile file,
        List<string> effectiveNames,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options,
        List<GDReflectionDroppedItem>? droppedByReflection,
        bool csharpReachable = false)
    {
        foreach (var symbol in semanticModel.GetMethods())
        {
            var methodName = symbol.Name;
            if (string.IsNullOrEmpty(methodName))
                continue;

            if (options.ShouldSkipMethod(methodName))
                continue;

            if (IsFrameworkMethod(file, methodName, options))
                continue;

            if (options.RespectSuppressionAnnotations && IsAnnotationSuppressed(semanticModel, methodName, options))
            {
                _annotationSuppressedCount++;
                continue;
            }

            bool isPrivate = methodName.StartsWith("_") && !options.ShouldSkipMethod(methodName);

            if (isPrivate && !options.IncludePrivate)
                continue;

            var (hasCallers, confidence, reason, reflSite) = CheckMethodCallers(
                file, effectiveNames, methodName, semanticModel, options);

            if (hasCallers && reflSite != null && droppedByReflection != null)
            {
                var posToken = symbol.PositionToken;
                droppedByReflection.Add(new GDReflectionDroppedItem
                {
                    Kind = GDDeadCodeKind.Function,
                    Name = methodName,
                    FilePath = file.FullPath ?? "",
                    Line = posToken?.StartLine ?? 0,
                    Column = posToken?.StartColumn ?? 0,
                    ReflectionKind = reflSite.Kind,
                    ReflectionSiteFile = reflSite.FilePath,
                    ReflectionSiteLine = reflSite.Line,
                    CallMethod = reflSite.CallMethod,
                    ReceiverTypeName = reflSite.ReceiverTypeName,
                    NameFilters = reflSite.NameFilters
                });
            }

            if (!hasCallers)
            {
                var reasonCode = confidence == GDReferenceConfidence.Strict
                    ? GDDeadCodeReasonCode.FNC
                    : GDDeadCodeReasonCode.FDT;

                if (file.IsGlobal && !isPrivate && options.TreatClassNameAsPublicAPI
                    && confidence == GDReferenceConfidence.Strict)
                {
                    confidence = GDReferenceConfidence.Potential;
                    reasonCode = GDDeadCodeReasonCode.FPA;
                }

                if (csharpReachable && !isPrivate && confidence == GDReferenceConfidence.Strict)
                {
                    confidence = GDReferenceConfidence.Potential;
                    reasonCode = GDDeadCodeReasonCode.CSI;
                }

                var posToken = symbol.PositionToken;
                var declNode = symbol.DeclarationNode;
                var endToken = declNode?.AllTokens.LastOrDefault();

                var item = new GDDeadCodeItem(GDDeadCodeKind.Function, methodName, file.FullPath ?? "")
                {
                    Line = posToken?.StartLine ?? 0,
                    Column = posToken?.StartColumn ?? 0,
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

    private bool HasCrossFileMemberAccess(GDScriptFile file, List<string> effectiveNames, string memberName)
    {
        if (effectiveNames.Count == 0)
            return false;

        var baseTypes = _projectModel.GetInheritanceChain(file);

        foreach (var otherFile in _project.ScriptFiles)
        {
            if (otherFile == file || otherFile.Class == null)
                continue;

            var otherModel = _projectModel.GetSemanticModel(otherFile);
            if (otherModel == null)
                continue;

            foreach (var name in effectiveNames)
            {
                if (otherModel.HasMemberAccessesIncludingDuckTyped(name, memberName))
                    return true;
            }

            foreach (var baseType in baseTypes)
            {
                if (otherModel.HasMemberAccessesIncludingDuckTyped(baseType, memberName))
                    return true;
            }
        }

        if (HasTresResourceReference(effectiveNames, baseTypes, memberName))
            return true;

        return false;
    }

    private bool HasTresResourceReference(List<string> effectiveNames, IReadOnlyList<string> baseTypes, string memberName)
    {
        var provider = _project.TresResourceProvider;
        if (provider == null)
            return false;

        if (provider.HasPropertyReference(effectiveNames, memberName))
            return true;

        if (baseTypes.Count > 0 && provider.HasPropertyReference((IList<string>)baseTypes, memberName))
            return true;

        return false;
    }

    private (bool HasCallers, GDReferenceConfidence Confidence, string Reason, GDReflectionCallSite? ReflSite) CheckMethodCallers(
        GDScriptFile file,
        List<string> effectiveNames,
        string methodName,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        if (_callSiteRegistry != null)
        {
            foreach (var name in effectiveNames)
            {
                var callers = _callSiteRegistry.GetCallersOf(name, methodName);
                if (callers.Count > 0)
                    return (true, GDReferenceConfidence.Strict, "Has callers", null);
            }
        }

        foreach (var name in effectiveNames)
        {
            var signalConnections = _signalRegistry.GetSignalsCallingMethod(name, methodName);
            if (signalConnections.Count > 0)
                return (true, GDReferenceConfidence.Strict, "Connected to signals", null);
        }

        var symbol = semanticModel.FindSymbol(methodName);
        if (symbol != null)
        {
            var refs = semanticModel.GetReferencesTo(symbol);
            if (refs.Count > 0)
            {
                return (true, GDReferenceConfidence.Strict, "Has local references", null);
            }
        }

        if (HasCrossFileMemberAccess(file, effectiveNames, methodName))
        {
            return (true, GDReferenceConfidence.Strict, "Has cross-file callers", null);
        }

        if (IsOverrideMethodUsed(file, methodName))
            return (true, GDReferenceConfidence.Strict, "Override of called base method", null);

        if (semanticModel.IsPropertyAccessor(methodName))
            return (true, GDReferenceConfidence.Strict, "Property accessor", null);

        if (IsPropertyAccessorInBaseClass(file, methodName))
            return (true, GDReferenceConfidence.Strict, "Property accessor (inherited)", null);

        if (IsCalledFromSubclass(file, effectiveNames, methodName))
            return (true, GDReferenceConfidence.Strict, "Called from subclass", null);

        var (reflectionReachable, reflSite) = IsReachableViaReflection(file, effectiveNames, methodName, GDReflectionKind.Method);
        if (reflectionReachable)
        {
            var reflFileName = System.IO.Path.GetFileName(reflSite!.FilePath);
            return (true, GDReferenceConfidence.Potential,
                $"Reachable via reflection at {reflFileName}:{reflSite.Line + 1}", reflSite);
        }

        if (_projectModel.IsMethodReferencedViaCallableStringFlow(methodName, effectiveNames))
        {
            return (true, GDReferenceConfidence.Strict, "Called via Callable with resolved string argument", null);
        }

        if (_projectModel.IsMethodReferencedViaExpressionDispatch(methodName, effectiveNames))
        {
            return (true, GDReferenceConfidence.Strict, "Called via Expression.execute() dispatch", null);
        }

        if (_callSiteRegistry != null)
        {
            var duckCallers = _callSiteRegistry.GetCallersOf("*", methodName);
            if (duckCallers.Count > 0)
            {
                return (false, GDReferenceConfidence.Potential,
                    $"May be called via duck-typing ({duckCallers.Count} potential site(s))", null);
            }
        }

        if (options.MaxConfidence >= GDReferenceConfidence.NameMatch && _callSiteRegistry != null)
        {
            var allTargets = _callSiteRegistry.GetAllTargets();
            var duckTypeCallsCount = allTargets.Count(t =>
                string.Equals(t.MethodName, methodName, StringComparison.OrdinalIgnoreCase));

            if (duckTypeCallsCount > 0)
            {
                return (false, GDReferenceConfidence.NameMatch,
                    $"May be called via duck-typing ({duckTypeCallsCount} potential sites)", null);
            }
        }

        return (false, GDReferenceConfidence.Strict, "No callers found", null);
    }

    private bool IsOverrideMethodUsed(GDScriptFile file, string methodName)
    {
        var chain = _projectModel.GetInheritanceChain(file);

        foreach (var parentTypeName in chain)
        {
            var parentFile = _project.GetScriptByTypeName(parentTypeName);
            if (parentFile == null)
                continue;

            var parentModel = _projectModel.GetSemanticModel(parentFile);
            if (parentModel == null)
                continue;

            var parentSymbol = parentModel.FindSymbol(methodName);
            if (parentSymbol == null || parentSymbol.Kind != GDSymbolKind.Method)
                continue;

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

            if (parentModel.GetReferencesTo(parentSymbol).Count > 0)
                return true;
        }

        return false;
    }

    private bool IsCalledFromSubclass(GDScriptFile file, List<string> effectiveNames, string methodName)
    {
        foreach (var otherFile in _project.ScriptFiles)
        {
            if (otherFile == file || otherFile.Class == null)
                continue;

            var otherModel = _projectModel.GetSemanticModel(otherFile);
            if (otherModel == null)
                continue;

            var otherBaseType = otherModel.BaseTypeName;
            if (string.IsNullOrEmpty(otherBaseType))
                continue;

            bool isSubclass = false;
            foreach (var name in effectiveNames)
            {
                if (string.Equals(otherBaseType, name, StringComparison.OrdinalIgnoreCase))
                {
                    isSubclass = true;
                    break;
                }
            }

            if (!isSubclass && file.FullPath != null)
            {
                var projectPath = _project.ProjectPath;
                if (!string.IsNullOrEmpty(projectPath) &&
                    file.FullPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                {
                    var relative = file.FullPath.Substring(projectPath.Length)
                        .TrimStart(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
                        .Replace('\\', '/');
                    var resPath = "res://" + relative;
                    if (string.Equals(otherBaseType, resPath, StringComparison.OrdinalIgnoreCase))
                        isSubclass = true;
                }
            }

            if (!isSubclass)
                continue;

            // Check if the subclass references (calls) this method
            var methodSymbol = otherModel.FindSymbol(methodName);
            if (methodSymbol != null && otherModel.GetReferencesTo(methodSymbol).Count > 0)
                return true;
        }

        return false;
    }

    private bool IsPropertyAccessorInBaseClass(GDScriptFile file, string methodName)
    {
        var chain = _projectModel.GetInheritanceChain(file);

        foreach (var parentTypeName in chain)
        {
            var parentFile = _project.GetScriptByTypeName(parentTypeName);
            if (parentFile == null)
                continue;

            var parentModel = _projectModel.GetSemanticModel(parentFile);
            if (parentModel == null)
                continue;

            if (parentModel.IsPropertyAccessor(methodName))
                return true;
        }

        return false;
    }

    private (bool IsReachable, GDReflectionCallSite? Site) IsReachableViaReflection(
        GDScriptFile file,
        List<string> effectiveNames,
        string memberName,
        GDReflectionKind kind)
    {
        foreach (var otherFile in _project.ScriptFiles)
        {
            var otherModel = _projectModel.GetSemanticModel(otherFile);
            if (otherModel == null) continue;

            foreach (var site in otherModel.GetReflectionCallSites())
            {
                if (site.Kind != kind)
                    continue;

                if (!site.Matches(memberName))
                    continue;

                if (site.IsSelfCall && otherFile == file)
                    return (true, site);

                if (site.ReceiverTypeName == "*")
                    return (true, site);

                foreach (var name in effectiveNames)
                {
                    if (string.Equals(site.ReceiverTypeName, name, StringComparison.OrdinalIgnoreCase))
                        return (true, site);
                }

                if (_projectModel.IsSubclassOf(file, site.ReceiverTypeName))
                    return (true, site);
            }
        }
        return (false, null);
    }

    private bool IsFrameworkMethod(
        GDScriptFile file,
        string methodName,
        GDDeadCodeOptions options)
    {
        if (options.FrameworkMethodPrefixes.Count == 0)
            return false;

        bool matchesPrefix = false;
        foreach (var prefix in options.FrameworkMethodPrefixes)
        {
            if (methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                matchesPrefix = true;
                break;
            }
        }

        if (!matchesPrefix)
            return false;

        if (options.FrameworkBaseClasses.Count == 0)
            return true;

        foreach (var baseClass in options.FrameworkBaseClasses)
        {
            if (_projectModel.IsSubclassOf(file, baseClass))
                return true;
        }

        return false;
    }

    private IEnumerable<GDDeadCodeItem> FindUnusedSignals(
        GDScriptFile file,
        List<string> effectiveNames,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options,
        List<GDReflectionDroppedItem>? droppedByReflection,
        bool csharpReachable = false)
    {
        foreach (var symbol in semanticModel.GetSignals())
        {
            var signalName = symbol.Name;
            if (string.IsNullOrEmpty(signalName))
                continue;

            if (semanticModel.HasWarningIgnore(signalName, "unused_signal"))
                continue;

            if (options.RespectSuppressionAnnotations && IsAnnotationSuppressed(semanticModel, signalName, options))
            {
                _annotationSuppressedCount++;
                continue;
            }

            bool isPrivate = signalName.StartsWith("_");

            if (isPrivate && !options.IncludePrivate)
                continue;

            bool isEmitted = semanticModel.IsSignalEmitted(signalName);

            if (!isEmitted)
            {
                isEmitted = _projectModel.IsSignalEmittedDynamically(file, file.Class!, signalName, effectiveNames);
            }

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

            if (!hasConnections)
            {
                var allConns = _signalRegistry.GetAllConnections();
                hasConnections = allConns.Any(c => c.SignalName == signalName &&
                    (effectiveNames.Any(n => c.EmitterType == n) || c.EmitterType == null));
            }

            if (!isEmitted && !hasConnections)
            {
                isEmitted = HasCrossFileMemberAccess(file, effectiveNames, signalName);
            }

            if (!isEmitted && !hasConnections)
            {
                var (sigReflReachable, sigReflSite) = IsReachableViaReflection(
                    file, effectiveNames, signalName, GDReflectionKind.Signal);
                if (sigReflReachable)
                {
                    if (droppedByReflection != null && sigReflSite != null)
                    {
                        var posToken = symbol.PositionToken;
                        droppedByReflection.Add(new GDReflectionDroppedItem
                        {
                            Kind = GDDeadCodeKind.Signal,
                            Name = signalName,
                            FilePath = file.FullPath ?? "",
                            Line = posToken?.StartLine ?? 0,
                            Column = posToken?.StartColumn ?? 0,
                            ReflectionKind = sigReflSite.Kind,
                            ReflectionSiteFile = sigReflSite.FilePath,
                            ReflectionSiteLine = sigReflSite.Line,
                            CallMethod = sigReflSite.CallMethod,
                            ReceiverTypeName = sigReflSite.ReceiverTypeName,
                            NameFilters = sigReflSite.NameFilters
                        });
                    }
                    continue;
                }
            }

            if (!isEmitted && !hasConnections)
            {
                var signalRefs = semanticModel.GetReferencesTo(symbol);
                if (signalRefs.Count > 0)
                    continue;
            }

            if (!isEmitted && !hasConnections)
            {
                var posToken = symbol.PositionToken;

                var signalConfidence = GDReferenceConfidence.Strict;
                var signalReasonCode = GDDeadCodeReasonCode.SNE;

                if (file.IsGlobal && !isPrivate && options.TreatClassNameAsPublicAPI)
                {
                    signalConfidence = GDReferenceConfidence.Potential;
                    signalReasonCode = GDDeadCodeReasonCode.FPA;
                }

                if (csharpReachable && !isPrivate && signalConfidence == GDReferenceConfidence.Strict)
                {
                    signalConfidence = GDReferenceConfidence.Potential;
                    signalReasonCode = GDDeadCodeReasonCode.CSI;
                }

                yield return new GDDeadCodeItem(GDDeadCodeKind.Signal, signalName, file.FullPath ?? "")
                {
                    Line = posToken?.StartLine ?? 0,
                    Column = posToken?.StartColumn ?? 0,
                    EndLine = posToken?.EndLine ?? 0,
                    EndColumn = posToken?.EndColumn ?? 0,
                    Confidence = signalConfidence,
                    ReasonCode = signalReasonCode,
                    Reason = "Signal is never emitted and has no connections",
                    IsPrivate = isPrivate
                };
            }
            else if (!isEmitted)
            {
                var posToken = symbol.PositionToken;

                yield return new GDDeadCodeItem(GDDeadCodeKind.Signal, signalName, file.FullPath ?? "")
                {
                    Line = posToken?.StartLine ?? 0,
                    Column = posToken?.StartColumn ?? 0,
                    EndLine = posToken?.EndLine ?? 0,
                    EndColumn = posToken?.EndColumn ?? 0,
                    Confidence = GDReferenceConfidence.Potential,
                    ReasonCode = GDDeadCodeReasonCode.SCB,
                    Reason = "Signal is connected but never emitted",
                    IsPrivate = isPrivate
                };
            }
        }
    }

    private IEnumerable<GDDeadCodeItem> FindUnusedParameters(
        GDScriptFile file,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        foreach (var methodSymbol in semanticModel.GetMethods())
        {
            var methodName = methodSymbol.Name;
            if (string.IsNullOrEmpty(methodName))
                continue;

            if (options.ShouldSkipMethod(methodName))
                continue;

            var paramSymbols = semanticModel.GetSymbolsOfKind(GDSymbolKind.Parameter)
                .Where(p => p.DeclaringScopeNode == methodSymbol.DeclarationNode)
                .ToList();

            foreach (var paramSymbol in paramSymbols)
            {
                var paramName = paramSymbol.Name;
                if (string.IsNullOrEmpty(paramName))
                    continue;

                if (paramName.StartsWith("_"))
                    continue;

                var refs = semanticModel.GetReferencesTo(paramSymbol);

                if (refs.Count == 0)
                {
                    var posToken = paramSymbol.PositionToken;

                    yield return new GDDeadCodeItem(GDDeadCodeKind.Parameter, paramName, file.FullPath ?? "")
                    {
                        Line = posToken?.StartLine ?? 0,
                        Column = posToken?.StartColumn ?? 0,
                        EndLine = posToken?.EndLine ?? 0,
                        EndColumn = posToken?.EndColumn ?? 0,
                        Confidence = GDReferenceConfidence.Strict,
                        ReasonCode = GDDeadCodeReasonCode.PNU,
                        Reason = $"Parameter is never used in method '{methodName}'",
                        IsPrivate = false
                    };
                }
            }
        }
    }

    private IEnumerable<GDDeadCodeItem> FindUnreachableCode(
        GDScriptFile file,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        foreach (var methodSymbol in semanticModel.GetMethods())
        {
            var methodName = methodSymbol.Name ?? "";

            foreach (var info in semanticModel.FindUnreachableStatements(methodName))
            {
                yield return new GDDeadCodeItem(GDDeadCodeKind.Unreachable, methodName, file.FullPath ?? "")
                {
                    Line = info.Line,
                    Column = info.Column,
                    EndLine = info.EndLine,
                    EndColumn = info.EndColumn,
                    Confidence = GDReferenceConfidence.Strict,
                    ReasonCode = GDDeadCodeReasonCode.UCR,
                    Reason = "Code is unreachable after return/break/continue",
                    IsPrivate = false
                };
            }
        }
    }

    private IEnumerable<GDDeadCodeItem> FindUnusedConstants(
        GDScriptFile file,
        List<string> effectiveNames,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options,
        bool csharpReachable = false)
    {
        foreach (var symbol in semanticModel.GetConstants())
        {
            var constName = symbol.Name;
            if (string.IsNullOrEmpty(constName))
                continue;

            if (options.RespectSuppressionAnnotations && IsAnnotationSuppressed(semanticModel, constName, options))
            {
                _annotationSuppressedCount++;
                continue;
            }

            bool isPrivate = constName.StartsWith("_");

            if (isPrivate && !options.IncludePrivate)
                continue;

            var references = semanticModel.GetReferencesTo(symbol);
            var reads = references.Where(r => r.IsRead).ToList();

            if (reads.Count == 0)
            {
                if (HasCrossFileMemberAccess(file, effectiveNames, constName))
                    continue;

                var posToken = symbol.PositionToken;

                var constConfidence = GDReferenceConfidence.Strict;
                var constReasonCode = GDDeadCodeReasonCode.CNU;

                if (file.IsGlobal && !isPrivate && options.TreatClassNameAsPublicAPI)
                {
                    constConfidence = GDReferenceConfidence.Potential;
                    constReasonCode = GDDeadCodeReasonCode.FPA;
                }

                if (csharpReachable && !isPrivate && constConfidence == GDReferenceConfidence.Strict)
                {
                    constConfidence = GDReferenceConfidence.Potential;
                    constReasonCode = GDDeadCodeReasonCode.CSI;
                }

                yield return new GDDeadCodeItem(GDDeadCodeKind.Constant, constName, file.FullPath ?? "")
                {
                    Line = posToken?.StartLine ?? 0,
                    Column = posToken?.StartColumn ?? 0,
                    EndLine = posToken?.EndLine ?? 0,
                    EndColumn = posToken?.EndColumn ?? 0,
                    Confidence = constConfidence,
                    ReasonCode = constReasonCode,
                    Reason = "Constant is never used",
                    IsPrivate = isPrivate
                };
            }
        }
    }

    private IEnumerable<GDDeadCodeItem> FindUnusedEnumValues(
        GDScriptFile file,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        foreach (var enumSymbol in semanticModel.GetEnums())
        {
            var enumName = enumSymbol.Name;

            var enumValues = semanticModel.GetSymbolsOfKind(GDSymbolKind.EnumValue)
                .Where(ev => ev.DeclaringScopeNode == enumSymbol.DeclarationNode)
                .ToList();

            foreach (var valueSymbol in enumValues)
            {
                var valueName = valueSymbol.Name;
                if (string.IsNullOrEmpty(valueName))
                    continue;

                var qualifiedName = !string.IsNullOrEmpty(enumName) ? $"{enumName}.{valueName}" : valueName;

                var isUsed = false;

                var refs = semanticModel.GetReferencesTo(valueSymbol);
                isUsed = refs.Count > 0;

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
                    var posToken = valueSymbol.PositionToken;

                    yield return new GDDeadCodeItem(GDDeadCodeKind.EnumValue, valueName, file.FullPath ?? "")
                    {
                        Line = posToken?.StartLine ?? 0,
                        Column = posToken?.StartColumn ?? 0,
                        EndLine = posToken?.EndLine ?? 0,
                        EndColumn = posToken?.EndColumn ?? 0,
                        Confidence = GDReferenceConfidence.Potential,
                        ReasonCode = GDDeadCodeReasonCode.ENU,
                        Reason = $"Enum value '{valueName}' in '{enumName ?? "anonymous"}' is never referenced",
                        IsPrivate = false
                    };
                }
            }
        }
    }

    private IEnumerable<GDDeadCodeItem> FindUnusedInnerClasses(
        GDScriptFile file,
        GDSemanticModel semanticModel,
        GDDeadCodeOptions options)
    {
        foreach (var symbol in semanticModel.GetInnerClasses())
        {
            var className = symbol.Name;
            if (string.IsNullOrEmpty(className))
                continue;

            if (options.RespectSuppressionAnnotations && IsAnnotationSuppressed(semanticModel, className, options))
            {
                _annotationSuppressedCount++;
                continue;
            }

            bool isPrivate = className.StartsWith("_");

            if (isPrivate && !options.IncludePrivate)
                continue;

            var isUsed = false;

            var refs = semanticModel.GetReferencesTo(symbol);
            isUsed = refs.Count > 0;

            if (!isUsed)
            {
                if (_callSiteRegistry != null)
                {
                    var callers = _callSiteRegistry.GetCallersOf(className, "new");
                    isUsed = callers.Count > 0;
                }
            }

            if (!isUsed)
            {
                var posToken = symbol.PositionToken;
                var declNode = symbol.DeclarationNode;
                var endToken = declNode?.AllTokens.LastOrDefault();

                yield return new GDDeadCodeItem(GDDeadCodeKind.InnerClass, className, file.FullPath ?? "")
                {
                    Line = posToken?.StartLine ?? 0,
                    Column = posToken?.StartColumn ?? 0,
                    EndLine = endToken?.EndLine ?? 0,
                    EndColumn = endToken?.EndColumn ?? 0,
                    Confidence = GDReferenceConfidence.Potential,
                    ReasonCode = GDDeadCodeReasonCode.ICU,
                    Reason = "Inner class is never instantiated or referenced",
                    IsPrivate = isPrivate
                };
            }
        }
    }

    private bool IsAnnotationSuppressed(GDSemanticModel semanticModel, string memberName, GDDeadCodeOptions options)
    {
        if (semanticModel.HasPublicApiAnnotation(memberName) || semanticModel.HasDynamicUseAnnotation(memberName))
            return true;

        foreach (var ann in options.CustomSuppressionAnnotations)
        {
            if (semanticModel.HasAnnotation(memberName, ann))
                return true;
        }

        return false;
    }

    #region Escape Analysis

    private static bool IsLocallyConstructedNonEscaping(
        GDSymbolInfo symbol,
        IReadOnlyList<GDReference> allReferences)
    {
        if (symbol.ScopeType != GDSymbolScopeType.LocalVariable)
            return false;

        if (!HasConstructorInitializer(symbol))
            return false;

        foreach (var writeRef in allReferences.Where(r => r.IsWrite))
        {
            if (!IsConstructorSourceAssignment(writeRef))
                return false;
        }

        foreach (var reference in allReferences)
        {
            if (reference.IsRead && !reference.IsPropertyWriteOnCaller)
                return false;
        }

        if (HasEscapeInAST(symbol))
            return false;

        return true;
    }

    private static bool HasConstructorInitializer(GDSymbolInfo symbol)
    {
        GDExpression? initializer = symbol.DeclarationNode switch
        {
            GDVariableDeclarationStatement local => local.Initializer,
            GDVariableDeclaration classVar => classVar.Initializer,
            _ => null
        };
        return initializer != null && IsConstructorExpression(initializer);
    }

    private static bool IsConstructorExpression(GDExpression expr)
    {
        if (expr is GDCallExpression call
            && call.CallerExpression is GDMemberOperatorExpression member
            && member.Identifier?.Sequence == "new")
            return true;

        if (expr is GDArrayInitializerExpression)
            return true;

        if (expr is GDDictionaryInitializerExpression)
            return true;

        if (expr is GDCallExpression ctorCall
            && ctorCall.CallerExpression is GDIdentifierExpression ctorId)
        {
            var name = ctorId.Identifier?.Sequence;
            if (!string.IsNullOrEmpty(name) && GDWellKnownTypes.IsBuiltInType(name))
                return true;
        }

        return false;
    }

    private static bool IsConstructorSourceAssignment(GDReference writeRef)
    {
        var node = writeRef.ReferenceNode;
        var parent = node?.Parent;
        while (parent != null)
        {
            if (parent is GDDualOperatorExpression dualOp)
            {
                return dualOp.RightExpression != null
                    && IsConstructorExpression(dualOp.RightExpression);
            }
            if (parent is GDVariableDeclarationStatement)
                return true;
            parent = parent.Parent;
        }
        return true;
    }

    private static bool HasEscapeInAST(GDSymbolInfo symbol)
    {
        var varName = symbol.Name;
        var scope = symbol.DeclaringScopeNode;
        if (scope == null || string.IsNullOrEmpty(varName))
            return true;

        foreach (var node in scope.AllNodes)
        {
            if (node is GDCallExpression call && call.Parameters != null)
            {
                foreach (var param in call.Parameters)
                {
                    if (ContainsIdentifier(param, varName))
                        return true;
                }
            }

            if (node is GDReturnExpression ret && ContainsIdentifier(ret.Expression, varName))
                return true;

            if (node is GDDualOperatorExpression dualOp)
            {
                var opType = dualOp.Operator?.OperatorType;
                if (opType == GDDualOperatorType.Assignment
                    || opType == GDDualOperatorType.AddAndAssign
                    || opType == GDDualOperatorType.SubtractAndAssign)
                {
                    if (dualOp.RightExpression != null
                        && ContainsIdentifier(dualOp.RightExpression, varName))
                    {
                        var lhsName = GetRootIdentifierName(dualOp.LeftExpression);
                        if (lhsName != varName)
                            return true;
                    }
                }
            }

            if (node is GDArrayInitializerExpression arr && arr.Values != null)
            {
                foreach (var val in arr.Values)
                {
                    if (ContainsIdentifier(val, varName))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsIdentifier(GDExpression? expr, string name)
    {
        if (expr == null) return false;
        if (expr is GDIdentifierExpression id && id.Identifier?.Sequence == name)
            return true;
        foreach (var child in expr.AllNodes)
        {
            if (child is GDIdentifierExpression childId && childId.Identifier?.Sequence == name)
                return true;
        }
        return false;
    }

    private static string? GetRootIdentifierName(GDExpression? expr)
    {
        while (expr is GDMemberOperatorExpression member)
            expr = member.CallerExpression;
        while (expr is GDIndexerExpression indexer)
            expr = indexer.CallerExpression;
        if (expr is GDIdentifierExpression ident)
            return ident.Identifier?.Sequence;
        return null;
    }

    #endregion
}
