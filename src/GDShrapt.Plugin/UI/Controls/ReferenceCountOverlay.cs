using Godot;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using SemanticsAnalyzer = GDShrapt.Semantics.GDScriptAnalyzer;

namespace GDShrapt.Plugin;

/// <summary>
/// Overlay control that displays reference counts above declarations in the editor.
/// Shows "Strict/Potential Type" format with different colors for each confidence level.
/// Types are computed asynchronously and appear with a smooth FadeIn animation.
/// Positioned over the TextEdit control and synchronized with scroll.
/// </summary>
internal partial class ReferenceCountOverlay : Control
{
    private TextEdit _textEdit;
    private GDScriptProject _scriptProject;
    private GDScriptFile _ScriptFile;

    private readonly Dictionary<int, ReferenceCountInfo> _referenceCountCache = new();
    private readonly Dictionary<int, float> _typeAlphaCache = new(); // FadeIn alpha per line (0.0 to 1.0)
    private readonly GDFindReferencesService _findReferencesService = new();
    private bool _needsRefresh = true;
    private double _refreshTimer = 0;
    private const double RefreshDelay = 1.0; // Debounce delay in seconds
    private const float FadeInSpeed = 3.0f; // Alpha increase per second (full fade in ~0.33s)

    // Async type inference
    private CancellationTokenSource _typeInferenceCts;
    private readonly object _cacheLock = new();

    // Colors for different reference confidence levels
    private static readonly Color StrictColor = new(0.3f, 0.8f, 0.3f, 0.9f);      // Green
    private static readonly Color PotentialColor = new(1.0f, 0.7f, 0.3f, 0.9f);   // Orange
    private static readonly Color SeparatorColor = new(0.5f, 0.5f, 0.5f, 0.8f);   // Gray
    private static readonly Color TypesButtonColor = new(0.4f, 0.6f, 0.9f, 0.8f); // Blue
    private static readonly Color InferredTypeColor = new(0.5f, 0.7f, 0.9f, 0.7f); // Light blue for inferred types
    private static readonly Color HoverColor = new(0.3f, 0.6f, 1.0f, 0.95f);      // Bright blue

    /// <summary>
    /// Event fired when user clicks on a reference count to see details.
    /// Parameters: symbolName, line
    /// </summary>
    public event Action<string, int> ReferenceCountClicked;

    /// <summary>
    /// Event fired when user clicks on the Types button to open Type Inference Panel.
    /// Parameters: symbolName, line
    /// </summary>
    public event Action<string, int> ShowTypeInferenceRequested;

    public override void _Ready()
    {
        Logger.Info("ReferenceCountOverlay: _Ready called - ENTRY");

        // Transparent background - we only draw text
        MouseFilter = MouseFilterEnum.Pass;

        // Explicitly enable processing - required for _Process to be called
        SetProcess(true);
        SetProcessInput(true);

        Logger.Info($"ReferenceCountOverlay: _Ready completed, ProcessMode={ProcessMode}, IsProcessing={IsProcessing()}");
    }

    /// <summary>
    /// Attaches the overlay to a TextEdit control.
    /// </summary>
    public void AttachToEditor(TextEdit textEdit, GDScriptProject ScriptProject)
    {
        Logger.Info($"ReferenceCountOverlay: AttachToEditor called, textEdit={textEdit != null}, project={ScriptProject != null}");

        if (_textEdit != null)
        {
            Detach();
        }

        _textEdit = textEdit;
        _scriptProject = ScriptProject;

        if (_textEdit != null)
        {
            // Position overlay to cover the TextEdit
            SetAnchorsPreset(LayoutPreset.FullRect);

            // Subscribe to text changes
            _textEdit.TextChanged += OnTextChanged;

            // Ensure processing is enabled (in case _Ready wasn't called yet)
            SetProcess(true);
            SetProcessInput(true);

            // We need to redraw when editor scrolls
            // TextEdit doesn't have a direct scroll signal, so we update in _Process

            _needsRefresh = true;
            Logger.Info($"ReferenceCountOverlay: Attached successfully, IsProcessing={IsProcessing()}, IsInsideTree={IsInsideTree()}");
        }
    }

    /// <summary>
    /// Sets the current script being edited.
    /// </summary>
    public void SetScript(GDScriptFile ScriptFile)
    {
        Logger.Info($"ReferenceCountOverlay: SetScript called, file={ScriptFile?.FullPath ?? "null"}, isInsideTree={IsInsideTree()}, isProcessing={IsProcessing()}");
        _ScriptFile = ScriptFile;
        _needsRefresh = true;
        _refreshTimer = 0.1; // Trigger refresh soon

        // Ensure processing is enabled
        if (!IsProcessing())
        {
            Logger.Info("ReferenceCountOverlay: SetScript - enabling processing");
            SetProcess(true);
        }

        QueueRedraw();

        // Also trigger a deferred refresh in case _Process isn't running yet
        // Use Callable.From to properly bind C# method for deferred call
        Callable.From(DeferredRefreshCheck).CallDeferred();
    }

    private void DeferredRefreshCheck()
    {
        Logger.Info($"ReferenceCountOverlay: DeferredRefreshCheck called, isProcessing={IsProcessing()}, isInsideTree={IsInsideTree()}, needsRefresh={_needsRefresh}");

        if (_needsRefresh && _ScriptFile != null)
        {
            Logger.Info("ReferenceCountOverlay: DeferredRefreshCheck - triggering immediate refresh");
            RefreshReferenceCounts();
        }
    }

    /// <summary>
    /// Detaches from the current TextEdit.
    /// </summary>
    public void Detach()
    {
        // Cancel any pending type inference
        _typeInferenceCts?.Cancel();
        _typeInferenceCts?.Dispose();
        _typeInferenceCts = null;

        if (_textEdit != null)
        {
            _textEdit.TextChanged -= OnTextChanged;
            _textEdit = null;
        }

        _ScriptFile = null;
        lock (_cacheLock)
        {
            _referenceCountCache.Clear();
            _typeAlphaCache.Clear();
        }
        QueueRedraw();
    }

    private void OnTextChanged()
    {
        _needsRefresh = true;
        _refreshTimer = RefreshDelay;
    }

    private int _processLogCounter = 0;

    public override void _Process(double delta)
    {
        if (_processLogCounter++ % 300 == 0)
            Logger.Info($"ReferenceCountOverlay: _Process called, textEdit={_textEdit != null}, visible={IsVisibleInTree()}, timer={_refreshTimer:F2}");

        if (_textEdit == null || !IsVisibleInTree())
            return;

        // Debounced refresh
        if (_refreshTimer > 0)
        {
            _refreshTimer -= delta;
            if (_refreshTimer <= 0)
            {
                Logger.Info("ReferenceCountOverlay: Timer expired, calling RefreshReferenceCounts");
                RefreshReferenceCounts();
            }
        }

        // Update FadeIn animations for type labels
        bool needsRedraw = UpdateTypeAlphas((float)delta);

        // Always redraw if animating, or periodically for scroll sync
        if (needsRedraw)
            QueueRedraw();
    }

    /// <summary>
    /// Updates alpha values for type FadeIn animations.
    /// Returns true if any alpha changed (needs redraw).
    /// </summary>
    private bool UpdateTypeAlphas(float delta)
    {
        bool anyChanged = false;

        lock (_cacheLock)
        {
            foreach (var kvp in _referenceCountCache)
            {
                var info = kvp.Value;
                int line = kvp.Key;

                // Only animate if type is available and not yet fully visible
                if (!string.IsNullOrEmpty(info.InferredType) && !info.IsTypeExplicit)
                {
                    if (!_typeAlphaCache.TryGetValue(line, out var alpha))
                    {
                        alpha = 0f;
                        _typeAlphaCache[line] = alpha;
                    }

                    if (alpha < 1.0f)
                    {
                        alpha = Math.Min(1.0f, alpha + FadeInSpeed * delta);
                        _typeAlphaCache[line] = alpha;
                        anyChanged = true;
                    }
                }
            }
        }

        return anyChanged;
    }

    private void RefreshReferenceCounts()
    {
        Logger.Info($"ReferenceCountOverlay: RefreshReferenceCounts called, scriptFile={_ScriptFile?.FullPath ?? "null"}, class={_ScriptFile?.Class != null}");

        // Cancel any pending type inference
        _typeInferenceCts?.Cancel();
        _typeInferenceCts?.Dispose();
        _typeInferenceCts = new CancellationTokenSource();
        var ct = _typeInferenceCts.Token;

        lock (_cacheLock)
        {
            _referenceCountCache.Clear();
            _typeAlphaCache.Clear();
        }

        if (_ScriptFile?.Class == null || _scriptProject == null)
        {
            Logger.Info("ReferenceCountOverlay: No script or project, skipping refresh");
            return;
        }

        // Find all declarations in the current script
        var declarations = FindDeclarations(_ScriptFile.Class);
        Logger.Info($"ReferenceCountOverlay: Found {declarations.Count} declarations");

        var declarationsWithRefs = new List<(DeclarationInfo decl, int strict, int potential)>();

        // Phase 1: Quickly compute reference counts (fast operation)
        foreach (var decl in declarations)
        {
            if (string.IsNullOrEmpty(decl.Name))
                continue;

            // Count references using semantic engine for accurate scope-aware counting
            var (strictCount, potentialCount) = CountReferencesWithSemantics(decl);

            if (strictCount > 0 || potentialCount > 0)
            {
                declarationsWithRefs.Add((decl, strictCount, potentialCount));

                // Add to cache immediately without type (will be filled async)
                lock (_cacheLock)
                {
                    _referenceCountCache[decl.Line] = new ReferenceCountInfo
                    {
                        SymbolName = decl.Name,
                        StrictCount = strictCount,
                        PotentialCount = potentialCount,
                        Line = decl.Line,
                        InferredType = null,
                        IsTypeExplicit = false
                    };
                }
            }
        }

        Logger.Info($"ReferenceCountOverlay: {declarationsWithRefs.Count} declarations with refs, cache size={_referenceCountCache.Count}");
        _needsRefresh = false;
        QueueRedraw();

        // Phase 2: Compute types asynchronously (can be slow)
        if (declarationsWithRefs.Count > 0)
        {
            // Capture analyzer reference for async operation
            var analyzer = _ScriptFile.Analyzer;

            Task.Run(() => ComputeTypesAsync(declarationsWithRefs, analyzer, ct), ct);
        }
    }

    /// <summary>
    /// Computes inferred types asynchronously for all declarations.
    /// Updates cache and triggers redraw when types become available.
    /// </summary>
    private void ComputeTypesAsync(
        List<(DeclarationInfo decl, int strict, int potential)> declarations,
        SemanticsAnalyzer? analyzer,
        CancellationToken ct)
    {
        try
        {
            var typeHelper = new GDTypeInferenceHelper(analyzer);

            foreach (var (decl, strictCount, potentialCount) in declarations)
            {
                if (ct.IsCancellationRequested)
                    return;

                // Extract type information from declaration
                var (inferredType, isExplicit) = GetTypeFromDeclaration(decl, typeHelper);

                // Update cache with type info
                lock (_cacheLock)
                {
                    if (_referenceCountCache.TryGetValue(decl.Line, out var info))
                    {
                        _referenceCountCache[decl.Line] = new ReferenceCountInfo
                        {
                            SymbolName = info.SymbolName,
                            StrictCount = info.StrictCount,
                            PotentialCount = info.PotentialCount,
                            Line = info.Line,
                            InferredType = inferredType,
                            IsTypeExplicit = isExplicit
                        };

                        // Reset alpha to 0 for FadeIn animation
                        if (!string.IsNullOrEmpty(inferredType) && !isExplicit)
                        {
                            _typeAlphaCache[decl.Line] = 0f;
                        }
                    }
                }
            }

            // Request redraw on main thread
            CallDeferred(MethodName.QueueRedraw);
        }
        catch (OperationCanceledException)
        {
            // Task was cancelled, ignore
        }
        catch (Exception ex)
        {
            Logger.Warning($"ReferenceCountOverlay: Type inference failed - {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts type information from a declaration.
    /// Returns (typeName, isExplicit) - isExplicit is true if type was explicitly annotated.
    /// </summary>
    private static (string? inferredType, bool isExplicit) GetTypeFromDeclaration(DeclarationInfo decl, GDTypeInferenceHelper typeHelper)
    {
        switch (decl.DeclarationNode)
        {
            case GDVariableDeclaration varDecl:
                // Check for explicit type annotation
                var explicitType = varDecl.Type?.BuildName();
                if (!string.IsNullOrEmpty(explicitType) && explicitType != "Variant")
                    return (explicitType, true);

                // Try to infer type from initializer
                var inferredVar = typeHelper.InferVariableType(varDecl);
                if (!inferredVar.IsUnknown)
                    return (inferredVar.TypeName, false);
                break;

            case GDParameterDeclaration paramDecl:
                // Check for explicit type annotation
                var paramType = paramDecl.Type?.BuildName();
                if (!string.IsNullOrEmpty(paramType) && paramType != "Variant")
                    return (paramType, true);

                // Try to infer from default value
                var inferredParam = typeHelper.InferParameterType(paramDecl);
                if (!inferredParam.IsUnknown)
                    return (inferredParam.TypeName, false);
                break;

            case GDMethodDeclaration methodDecl:
                // For methods, show return type
                var returnType = methodDecl.ReturnType?.BuildName();
                if (!string.IsNullOrEmpty(returnType) && returnType != "Variant")
                    return (returnType, true);
                // Could infer return type from body, but that's complex - skip for now
                break;

            case GDSignalDeclaration:
                // Signals don't have types
                return (null, true);

            case GDEnumDeclaration:
                // Enums are their own type
                return (decl.Name, true);

            case GDInnerClassDeclaration:
                // Inner classes are their own type
                return (decl.Name, true);
        }

        return (null, false);
    }

    private List<DeclarationInfo> FindDeclarations(GDClassDeclaration classDecl)
    {
        var declarations = new List<DeclarationInfo>();

        foreach (var token in classDecl.AllTokens)
        {
            if (token is not GDIdentifier identifier)
                continue;

            var parent = identifier.Parent;

            // Check if it's a declaration and capture the declaration node
            GDNode? declNode = parent switch
            {
                GDMethodDeclaration m => m,
                GDVariableDeclaration v => v,
                GDSignalDeclaration s => s,
                GDParameterDeclaration p => p,
                GDEnumDeclaration e => e,
                GDInnerClassDeclaration i => i,
                _ => null
            };

            if (declNode != null)
            {
                declarations.Add(new DeclarationInfo
                {
                    Name = identifier.Sequence,
                    Line = identifier.StartLine,
                    Column = identifier.StartColumn,
                    Identifier = identifier,
                    DeclarationNode = declNode
                });
            }
        }

        // Deduplicate by line (in case of multiple identifiers on same line)
        return declarations
            .GroupBy(d => d.Line)
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>
    /// Counts references using the semantic engine for accurate scope-aware counting.
    /// Returns tuple of (strictCount, potentialCount).
    /// </summary>
    private (int strictCount, int potentialCount) CountReferencesWithSemantics(DeclarationInfo decl)
    {
        if (_ScriptFile?.Class == null || decl.Identifier == null)
        {
            var simple = CountReferencesSimple(decl.Name);
            return (simple, 0);
        }

        // Create a GDScriptFile wrapper for refactoring context
        var reference = new GDScriptReference(_ScriptFile.FullPath ?? "unknown.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(_ScriptFile.Class.ToString());

        // Build cursor position and selection info for the context
        var cursor = new GDCursorPosition(decl.Line, decl.Column);
        var selection = GDSelectionInfo.None;

        // Create refactoring context for semantics service
        var context = new GDRefactoringContext(
            scriptFile,
            _ScriptFile.Class,
            cursor,
            selection);

        // Determine symbol scope using the identifier from declaration
        var scope = _findReferencesService.DetermineSymbolScope(decl.Identifier, context);
        if (scope == null)
        {
            var simple = CountReferencesSimple(decl.Name);
            return (simple, 0);
        }

        // For local/parameter/for-loop scope - count only within their scope (all strict)
        if (scope.Type == GDSymbolScopeType.LocalVariable ||
            scope.Type == GDSymbolScopeType.MethodParameter ||
            scope.Type == GDSymbolScopeType.ForLoopVariable ||
            scope.Type == GDSymbolScopeType.MatchCaseVariable)
        {
            var result = _findReferencesService.FindReferencesForScope(context, scope);
            return (result.StrictReferences.Count, result.PotentialReferences.Count);
        }

        // For class members - use cross-file search with type awareness
        if (scope.Type == GDSymbolScopeType.ClassMember)
        {
            return CountClassMemberReferences(scope, decl.Name);
        }

        // Fallback for project-wide symbols
        var fallbackSimple = CountReferencesSimple(decl.Name);
        return (fallbackSimple, 0);
    }

    /// <summary>
    /// Counts references for a class member, including cross-file references with type inference.
    /// Returns tuple of (strictCount, potentialCount).
    /// </summary>
    private (int strictCount, int potentialCount) CountClassMemberReferences(GDSymbolScope scope, string symbolName)
    {
        int strictCount = 0;
        int potentialCount = 0;

        // Count in current file (all identifiers with this name are strict in same file)
        if (scope.ContainingClass != null)
        {
            strictCount += scope.ContainingClass.AllTokens.OfType<GDIdentifier>()
                .Count(i => i.Sequence == symbolName);
        }

        // For public members, also count cross-file references with type checking
        if (scope.IsPublic && !string.IsNullOrEmpty(_ScriptFile?.TypeName))
        {
            var typeName = _ScriptFile.TypeName;

            foreach (var script in _scriptProject.ScriptFiles)
            {
                if (script == _ScriptFile || script.Class == null)
                    continue;

                var analyzer = script.Analyzer;

                // Find member access expressions like: instance.symbolName
                foreach (var memberOp in script.Class.AllNodes.OfType<GDMemberOperatorExpression>())
                {
                    if (memberOp.Identifier?.Sequence != symbolName)
                        continue;

                    // Use type inference to verify this is actually a reference to our type
                    if (analyzer != null)
                    {
                        var callerType = analyzer.GetTypeForNode(memberOp.CallerExpression);
                        if (callerType == typeName)
                        {
                            // Type is known and matches - strict reference
                            strictCount++;
                        }
                        else if (callerType == "Variant" || string.IsNullOrEmpty(callerType))
                        {
                            // Type is unknown/Variant - potential reference (name match only)
                            potentialCount++;
                        }
                        // If type is known but different, skip (not our symbol)
                    }
                    else
                    {
                        // No analyzer available - count as potential
                        potentialCount++;
                    }
                }
            }
        }

        return (strictCount, potentialCount);
    }

    /// <summary>
    /// Simple fallback counting by name (used when semantic analysis is not available).
    /// </summary>
    private int CountReferencesSimple(string symbolName)
    {
        int count = 0;

        foreach (var script in _scriptProject.ScriptFiles)
        {
            if (script.Class == null)
                continue;

            foreach (var token in script.Class.AllTokens)
            {
                if (token is GDIdentifier id && id.Sequence == symbolName)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private int _drawLogCounter = 0;

    public override void _Draw()
    {
        if (_textEdit == null)
        {
            if (_drawLogCounter++ % 100 == 0)
                Logger.Info("ReferenceCountOverlay: _Draw called but _textEdit is null");
            return;
        }

        // Lock cache for reading
        Dictionary<int, ReferenceCountInfo> cacheCopy;
        Dictionary<int, float> alphaCopy;
        lock (_cacheLock)
        {
            if (_referenceCountCache.Count == 0)
            {
                if (_drawLogCounter++ % 100 == 0)
                    Logger.Info("ReferenceCountOverlay: _Draw called but cache is empty");
                return;
            }

            cacheCopy = new Dictionary<int, ReferenceCountInfo>(_referenceCountCache);
            alphaCopy = new Dictionary<int, float>(_typeAlphaCache);
        }

        var font = ThemeDB.FallbackFont;
        var fontSize = 11;

        // Get visible line range
        int firstVisibleLine = _textEdit.GetFirstVisibleLine();
        int lastVisibleLine = firstVisibleLine + _textEdit.GetVisibleLineCount() + 1;

        // Get the line height
        float lineHeight = _textEdit.GetLineHeight();

        // Get current mouse position for hover detection
        var mousePos = GetLocalMousePosition();

        int drawnCount = 0;
        foreach (var kvp in cacheCopy)
        {
            int line = kvp.Key;

            // Skip if not visible
            if (line < firstVisibleLine || line > lastVisibleLine)
                continue;

            var info = kvp.Value;

            // Get alpha for type FadeIn (default to 1.0 if not in cache)
            float typeAlpha = alphaCopy.TryGetValue(line, out var alpha) ? alpha : 1.0f;

            // Calculate position - above the line
            float yPos = (line - firstVisibleLine) * lineHeight - 2;

            // Offset for the gutter/line numbers (estimate)
            float xPos = 60;

            // Draw reference count in "N/M Type" format
            DrawReferenceCount(font, fontSize, xPos, yPos, info, mousePos, typeAlpha);
            drawnCount++;
        }

        if (_drawLogCounter++ % 100 == 0)
            Logger.Info($"ReferenceCountOverlay: _Draw completed, drew {drawnCount} items, visible lines {firstVisibleLine}-{lastVisibleLine}, lineHeight={lineHeight}");
    }

    private void DrawReferenceCount(Font font, int fontSize, float xPos, float yPos, ReferenceCountInfo info, Vector2 mousePos, float typeAlpha)
    {
        float currentX = xPos;

        // Check if mouse is near for hover effect
        var totalText = FormatReferenceCountText(info);
        var totalSize = font.GetStringSize(totalText, HorizontalAlignment.Left, -1, fontSize);
        var textRect = new Rect2(xPos, yPos - fontSize, totalSize.X + 80, fontSize + 4); // +80 for type and [T] button
        bool isHovered = textRect.HasPoint(mousePos);

        // Format: "N/M Type" where N is strict (green), M is potential (orange), Type is inferred type (light blue if inferred)
        // Example: "2/3 String | Player" or "5 int"

        // Draw reference counts
        if (info.PotentialCount > 0)
        {
            // Format: "N/M" where N is strict (green) and M is potential (orange)
            var strictText = info.StrictCount.ToString();
            var separatorText = "/";
            var potentialText = info.PotentialCount.ToString();

            // Draw strict count (green)
            DrawString(font, new Vector2(currentX, yPos), strictText, HorizontalAlignment.Left, -1, fontSize,
                isHovered ? HoverColor : StrictColor);
            currentX += font.GetStringSize(strictText, HorizontalAlignment.Left, -1, fontSize).X;

            // Draw separator (gray)
            DrawString(font, new Vector2(currentX, yPos), separatorText, HorizontalAlignment.Left, -1, fontSize,
                SeparatorColor);
            currentX += font.GetStringSize(separatorText, HorizontalAlignment.Left, -1, fontSize).X;

            // Draw potential count (orange)
            DrawString(font, new Vector2(currentX, yPos), potentialText, HorizontalAlignment.Left, -1, fontSize,
                isHovered ? HoverColor : PotentialColor);
            currentX += font.GetStringSize(potentialText, HorizontalAlignment.Left, -1, fontSize).X;
        }
        else
        {
            // Only strict references - show simple count in green
            var text = info.StrictCount.ToString();
            DrawString(font, new Vector2(currentX, yPos), text, HorizontalAlignment.Left, -1, fontSize,
                isHovered ? HoverColor : StrictColor);
            currentX += font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize).X;
        }

        // Draw inferred type with FadeIn animation (only if not explicitly annotated and type is known)
        if (!string.IsNullOrEmpty(info.InferredType) && !info.IsTypeExplicit && typeAlpha > 0.01f)
        {
            // Add space before type
            var typeText = $" {info.InferredType}";

            // Apply FadeIn alpha to the inferred type color
            var typeColor = new Color(InferredTypeColor.R, InferredTypeColor.G, InferredTypeColor.B, InferredTypeColor.A * typeAlpha);

            DrawString(font, new Vector2(currentX, yPos), typeText, HorizontalAlignment.Left, -1, fontSize,
                typeColor);
            currentX += font.GetStringSize(typeText, HorizontalAlignment.Left, -1, fontSize).X;
        }

        // Draw [T] button for Type Inference Panel
        var typesButtonText = " [T]";
        var typesButtonRect = new Rect2(
            currentX,
            yPos - fontSize,
            font.GetStringSize(typesButtonText, HorizontalAlignment.Left, -1, fontSize).X,
            fontSize + 4);

        bool typesButtonHovered = typesButtonRect.HasPoint(mousePos);
        DrawString(font, new Vector2(currentX, yPos), typesButtonText, HorizontalAlignment.Left, -1, fontSize,
            typesButtonHovered ? HoverColor : TypesButtonColor);
    }

    private static string FormatReferenceCountText(ReferenceCountInfo info)
    {
        if (info.PotentialCount > 0)
        {
            return $"{info.StrictCount}/{info.PotentialCount}";
        }
        return $"{info.StrictCount}";
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton &&
            mouseButton.ButtonIndex == MouseButton.Left &&
            mouseButton.Pressed)
        {
            // Check if clicked on any reference count or types button
            var clickPos = mouseButton.Position;
            var font = ThemeDB.FallbackFont;
            var fontSize = 11;

            int firstVisibleLine = _textEdit?.GetFirstVisibleLine() ?? 0;
            float lineHeight = _textEdit?.GetLineHeight() ?? 20;

            foreach (var kvp in _referenceCountCache)
            {
                int line = kvp.Key;
                var info = kvp.Value;

                float yPos = (line - firstVisibleLine) * lineHeight - 2;
                float xPos = 60;

                // Calculate text positions
                var text = FormatReferenceCountText(info);
                var textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
                var textRect = new Rect2(xPos, yPos - fontSize, textSize.X, fontSize + 4);

                // Calculate [T] button position
                var typesButtonText = " [T]";
                float typesButtonX = xPos + textSize.X;
                var typesButtonSize = font.GetStringSize(typesButtonText, HorizontalAlignment.Left, -1, fontSize);
                var typesButtonRect = new Rect2(typesButtonX, yPos - fontSize, typesButtonSize.X, fontSize + 4);

                // Check [T] button click first
                if (typesButtonRect.HasPoint(clickPos))
                {
                    Logger.Info($"Type inference panel requested for '{info.SymbolName}' at line {line}");
                    ShowTypeInferenceRequested?.Invoke(info.SymbolName, line);
                    AcceptEvent();
                    return;
                }

                // Check reference count click
                if (textRect.HasPoint(clickPos))
                {
                    Logger.Info($"Reference count clicked for '{info.SymbolName}' at line {line}");
                    ReferenceCountClicked?.Invoke(info.SymbolName, line);
                    AcceptEvent();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Forces a refresh of reference counts.
    /// </summary>
    public void ForceRefresh()
    {
        _needsRefresh = true;
        _refreshTimer = 0;
        RefreshReferenceCounts();
    }

    private struct DeclarationInfo
    {
        public string Name;
        public int Line;
        public int Column;
        public GDIdentifier Identifier;
        public GDNode DeclarationNode;    // The full declaration node
    }

    private struct ReferenceCountInfo
    {
        public string SymbolName;
        public int StrictCount;
        public int PotentialCount;
        public int Line;
        public string InferredType;      // The inferred type (if not explicit)
        public bool IsTypeExplicit;       // True if type is explicitly declared
    }
}
