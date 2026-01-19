using Godot;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GDShrapt.Plugin;

/// <summary>
/// Manages custom gutters in CodeEdit for displaying reference counts and error indicators.
/// Uses native CodeEdit gutter API for proper positioning and click handling.
/// Format: ◆ N  [icon]
/// - ◆ = strict refs (green), ◇ = potential only (yellow)
/// - N = total reference count (yellowish if potential refs exist)
/// - [icon] = info button to open TypeInferencePanel
/// </summary>
internal class GDGutterManager
{
    private CodeEdit? _codeEdit;
    private GDScriptProject? _scriptProject;
    private GDScriptFile? _scriptFile;

    private int _refCountGutterId = -1;
    private int _errorGutterId = -1;

    // Reference data: line -> info
    private readonly Dictionary<int, ReferenceInfo> _references = new();
    private readonly object _referencesLock = new();

    // Diagnostic data: line -> info
    private readonly Dictionary<int, DiagnosticDisplayInfo> _diagnostics = new();
    private readonly object _diagnosticsLock = new();

    // Services
    private readonly GDFindReferencesService _findReferencesService = new();

    // Async type inference
    private CancellationTokenSource? _typeInferenceCts;

    // Settings
    private bool _referencesEnabled = true;
    private bool _errorsEnabled = true;
    private bool _errorLineBackgroundEnabled = true;

    // Hover state
    private int _hoverLine = -1;
    private float _lastMouseX = 0;

    // Icon button zone (right side of gutter)
    private const float IconZoneWidth = 20f;

    // Bright colors for contrast
    private static readonly Color RefIconStrictColor = new(0.31f, 0.79f, 0.69f);     // #4EC9B0 green - ◆
    private static readonly Color RefIconPotentialColor = new(0.86f, 0.86f, 0.67f);   // #DCDCAA yellow - ◇
    private static readonly Color RefIconMixedColor = new(0.45f, 0.82f, 0.6f);        // #73D199 yellowish-green (mixed)
    private static readonly Color InfoIconColor = new(0.6f, 0.7f, 0.9f);              // Light blue for info icon

    // Error gutter colors
    private static readonly Color ErrorColor = new(1.0f, 0.3f, 0.3f, 0.9f);        // Red
    private static readonly Color WarningColor = new(1.0f, 0.8f, 0.2f, 0.9f);      // Yellow
    private static readonly Color HintColor = new(0.4f, 0.7f, 1.0f, 0.9f);         // Blue
    private static readonly Color InfoColor = new(0.5f, 0.5f, 0.5f, 0.7f);         // Gray

    // Line background colors for errors
    private static readonly Color ErrorBgColor = new(0.5f, 0.1f, 0.1f, 0.3f);      // Semi-transparent red
    private static readonly Color WarningBgColor = new(0.5f, 0.4f, 0.1f, 0.2f);    // Semi-transparent yellow

    /// <summary>
    /// Event fired when user clicks on reference counts (n/m).
    /// Parameters: symbolName, line
    /// </summary>
    public event Action<string, int>? ReferencesClicked;

    /// <summary>
    /// Event fired when user clicks on type (-> Type).
    /// Parameters: symbolName, line, scriptFile
    /// </summary>
    public event Action<string, int, GDScriptFile?>? TypeClicked;

    /// <summary>
    /// Event fired when user clicks on an error/diagnostic gutter.
    /// Parameters: line, diagnosticCode
    /// </summary>
    public event Action<int, string>? DiagnosticClicked;

    /// <summary>
    /// Attaches the gutter manager to a CodeEdit control.
    /// </summary>
    public void AttachToEditor(CodeEdit codeEdit, GDScriptProject? project)
    {
        if (_codeEdit != null)
        {
            Detach();
        }

        _codeEdit = codeEdit;
        _scriptProject = project;

        if (_codeEdit == null)
            return;

        Logger.Info("GDGutterManager: Attaching to CodeEdit");

        // Add reference count gutter at position 0 (leftmost)
        // Width 60px for "◆ 99 [icon]" format
        if (_referencesEnabled)
        {
            _refCountGutterId = 0;
            _codeEdit.AddGutter(_refCountGutterId);
            _codeEdit.SetGutterWidth(_refCountGutterId, 60);
            _codeEdit.SetGutterType(_refCountGutterId, TextEdit.GutterType.Custom);
            _codeEdit.SetGutterClickable(_refCountGutterId, true);
            _codeEdit.SetGutterOverwritable(_refCountGutterId, false);
            _codeEdit.SetGutterName(_refCountGutterId, "gdshrapt_refs");
            _codeEdit.SetGutterCustomDraw(_refCountGutterId,
                Callable.From<int, int, Rect2>(DrawReferencesGutter));

            Logger.Info($"GDGutterManager: Created references gutter with id={_refCountGutterId}");
        }

        // Add error indicator gutter (after references)
        if (_errorsEnabled)
        {
            _errorGutterId = _referencesEnabled ? 1 : 0;
            _codeEdit.AddGutter(_errorGutterId);
            _codeEdit.SetGutterWidth(_errorGutterId, 24);  // Wider for icon + count
            _codeEdit.SetGutterType(_errorGutterId, TextEdit.GutterType.Custom);
            _codeEdit.SetGutterClickable(_errorGutterId, true);
            _codeEdit.SetGutterOverwritable(_errorGutterId, false);
            _codeEdit.SetGutterName(_errorGutterId, "gdshrapt_errors");
            _codeEdit.SetGutterCustomDraw(_errorGutterId,
                Callable.From<int, int, Rect2>(DrawErrorGutter));

            Logger.Info($"GDGutterManager: Created errors gutter with id={_errorGutterId}");
        }

        // Connect click handler
        _codeEdit.GutterClicked += OnGutterClicked;

        // Connect GUI input for hover detection
        _codeEdit.GuiInput += OnGuiInput;

        Logger.Info("GDGutterManager: Attached successfully");
    }

    /// <summary>
    /// Sets whether the reference count gutter is enabled.
    /// </summary>
    public void SetReferencesEnabled(bool enabled)
    {
        _referencesEnabled = enabled;
    }

    /// <summary>
    /// Sets whether the error indicator gutter is enabled.
    /// </summary>
    public void SetErrorsEnabled(bool enabled)
    {
        _errorsEnabled = enabled;
    }

    /// <summary>
    /// Sets whether error line background highlighting is enabled.
    /// </summary>
    public void SetErrorLineBackgroundEnabled(bool enabled)
    {
        _errorLineBackgroundEnabled = enabled;
        // Re-apply diagnostics to update backgrounds
        if (_codeEdit != null)
        {
            UpdateLineBackgrounds();
        }
    }

    /// <summary>
    /// Detaches from the current CodeEdit.
    /// </summary>
    public void Detach()
    {
        // Cancel pending operations
        _typeInferenceCts?.Cancel();
        _typeInferenceCts?.Dispose();
        _typeInferenceCts = null;

        if (_codeEdit != null)
        {
            _codeEdit.GutterClicked -= OnGutterClicked;
            _codeEdit.GuiInput -= OnGuiInput;

            // Clear line backgrounds
            ClearLineBackgrounds();

            // Remove gutters in reverse order (error first, then refs) to maintain correct indices
            if (_errorGutterId >= 0)
            {
                _codeEdit.RemoveGutter(_errorGutterId);
                _errorGutterId = -1;
            }

            if (_refCountGutterId >= 0)
            {
                _codeEdit.RemoveGutter(_refCountGutterId);
                _refCountGutterId = -1;
            }

            _codeEdit = null;
        }

        _scriptFile = null;
        _scriptProject = null;

        lock (_referencesLock)
        {
            _references.Clear();
        }

        lock (_diagnosticsLock)
        {
            _diagnostics.Clear();
        }

        Logger.Info("GDGutterManager: Detached");
    }

    /// <summary>
    /// Sets the current script and refreshes reference counts.
    /// </summary>
    public void SetScript(GDScriptFile? scriptFile)
    {
        _scriptFile = scriptFile;

        if (scriptFile != null && _referencesEnabled)
        {
            RefreshReferenceCounts();
        }
    }

    /// <summary>
    /// Updates diagnostics from DiagnosticService (uses plugin's Diagnostic type).
    /// </summary>
    public void SetDiagnostics(IReadOnlyList<GDPluginDiagnostic>? diagnostics)
    {
        lock (_diagnosticsLock)
        {
            _diagnostics.Clear();

            if (diagnostics != null)
            {
                // Group diagnostics by line and count them
                var byLine = diagnostics.GroupBy(d => d.StartLine);

                foreach (var group in byLine)
                {
                    int line = group.Key;
                    var diagList = group.ToList();
                    var highestSeverity = diagList.OrderByDescending(d => GetSeverityPriority(d.Severity)).First();

                    _diagnostics[line] = new DiagnosticDisplayInfo
                    {
                        Code = highestSeverity.RuleId,
                        Message = highestSeverity.Message,
                        Severity = highestSeverity.Severity,
                        Line = line,
                        Count = diagList.Count
                    };
                }
            }
        }

        // Update line backgrounds
        UpdateLineBackgrounds();

        // Request redraw
        _codeEdit?.QueueRedraw();

        Logger.Debug($"GDGutterManager: Updated diagnostics, count={_diagnostics.Count}");
    }

    /// <summary>
    /// Forces a refresh of reference counts.
    /// </summary>
    public void ForceRefresh()
    {
        RefreshReferenceCounts();
    }

    #region Drawing

    /// <summary>
    /// Draws the reference count gutter for a line.
    /// Format: ◆ N  [icon]
    /// - ◆ = strict refs (green), ◇ = potential only (yellow)
    /// - N = total reference count (yellowish if potential refs exist)
    /// - [icon] = info button (right side)
    /// </summary>
    private void DrawReferencesGutter(int line, int gutterId, Rect2 area)
    {
        if (_codeEdit == null || gutterId != _refCountGutterId)
            return;

        ReferenceInfo info;
        lock (_referencesLock)
        {
            if (!_references.TryGetValue(line, out info))
                return;
        }

        var font = _codeEdit.GetThemeFont("font");
        var fontSize = _codeEdit.GetThemeFontSize("font_size") - 2;
        float x = area.Position.X + 2;
        float y = area.Position.Y + (area.Size.Y + fontSize) / 2;
        var gutterWidth = _codeEdit.GetGutterWidth(_refCountGutterId);

        // Determine hover state for this line
        bool isHoveredLine = (_hoverLine == line);

        // Zone 1: Reference icon + count (◆ N or ◇ N)
        bool hasStrictRefs = info.StrictCount > 0;
        bool hasPotentialRefs = info.PotentialCount > 0;
        bool hasRefs = hasStrictRefs || hasPotentialRefs;

        if (hasRefs)
        {
            // Icon: ◆ for strict refs, ◇ for potential only
            var icon = hasStrictRefs ? "◆" : "◇";
            var iconColor = hasStrictRefs ? RefIconStrictColor : RefIconPotentialColor;
            iconColor = ApplyHover(iconColor, isHoveredLine);
            _codeEdit.DrawString(font, new Vector2(x, y), icon,
                HorizontalAlignment.Left, -1, fontSize, iconColor);
            x += font.GetStringSize(icon, HorizontalAlignment.Left, -1, fontSize).X + 2;

            // Total count (strict + potential)
            // Color: yellowish if potential refs exist alongside strict refs
            var totalCount = info.StrictCount + info.PotentialCount;
            var countText = totalCount.ToString();
            Color countColor;
            if (hasStrictRefs && hasPotentialRefs)
            {
                countColor = RefIconMixedColor;  // Yellowish-green for mixed
            }
            else if (hasStrictRefs)
            {
                countColor = RefIconStrictColor;  // Green for strict only
            }
            else
            {
                countColor = RefIconPotentialColor;  // Yellow for potential only
            }
            countColor = ApplyHover(countColor, isHoveredLine);
            _codeEdit.DrawString(font, new Vector2(x, y), countText,
                HorizontalAlignment.Left, -1, fontSize, countColor);
        }

        // Zone 2: Info icon (right side of gutter)
        // Draw a small info icon to open TypeInferencePanel
        var infoIcon = "ⓘ";  // Unicode info symbol (softer than emoji)
        var infoColor = ApplyHover(InfoIconColor, isHoveredLine);
        var infoWidth = font.GetStringSize(infoIcon, HorizontalAlignment.Left, -1, fontSize).X;
        var infoX = gutterWidth - infoWidth - 4;
        _codeEdit.DrawString(font, new Vector2(infoX, y), infoIcon,
            HorizontalAlignment.Left, -1, fontSize, infoColor);
    }

    /// <summary>
    /// Applies hover brightness (+30%) to a color.
    /// </summary>
    private static Color ApplyHover(Color color, bool isHovered)
    {
        if (!isHovered) return color;
        return new Color(
            Mathf.Min(color.R * 1.3f, 1.0f),
            Mathf.Min(color.G * 1.3f, 1.0f),
            Mathf.Min(color.B * 1.3f, 1.0f),
            color.A
        );
    }

    /// <summary>
    /// Draws the error indicator gutter for a line.
    /// Shows icon + count for multiple errors on the same line.
    /// </summary>
    private void DrawErrorGutter(int line, int gutterId, Rect2 area)
    {
        if (_codeEdit == null || gutterId != _errorGutterId)
            return;

        DiagnosticDisplayInfo info;
        lock (_diagnosticsLock)
        {
            if (!_diagnostics.TryGetValue(line, out info))
                return;
        }

        // Get color based on severity
        Color color = info.Severity switch
        {
            GDDiagnosticSeverity.Error => ErrorColor,
            GDDiagnosticSeverity.Warning => WarningColor,
            GDDiagnosticSeverity.Hint => HintColor,
            _ => InfoColor
        };

        // Draw a circle indicator
        float radius = 5;
        var circleCenter = new Vector2(area.Position.X + 8, area.Position.Y + area.Size.Y / 2);
        _codeEdit.DrawCircle(circleCenter, radius, color);

        // If multiple diagnostics on this line, show count
        if (info.Count > 1)
        {
            var font = _codeEdit.GetThemeFont("font");
            var fontSize = _codeEdit.GetThemeFontSize("font_size") - 4;
            var countText = info.Count.ToString();
            float textX = circleCenter.X + radius + 2;
            float textY = area.Position.Y + (area.Size.Y + fontSize) / 2;
            _codeEdit.DrawString(font, new Vector2(textX, textY), countText,
                HorizontalAlignment.Left, -1, fontSize, color);
        }
    }

    #endregion

    #region Hover Handling

    private void OnGuiInput(InputEvent @event)
    {
        if (_codeEdit == null || _refCountGutterId < 0)
            return;

        var gutterWidth = _codeEdit.GetGutterWidth(_refCountGutterId);

        if (@event is InputEventMouseButton mouseBtn)
        {
            if (mouseBtn.ButtonIndex == MouseButton.Left)
            {
                _lastMouseX = mouseBtn.Position.X;
            }
        }
        else if (@event is InputEventMouseMotion motion)
        {
            // Update hover state for content
            UpdateHoverState(motion.Position);
        }
    }

    private void UpdateHoverState(Vector2 mousePos)
    {
        if (_codeEdit == null)
            return;

        // Check if mouse is within gutter area
        var gutterWidth = _codeEdit.GetGutterWidth(_refCountGutterId);
        if (mousePos.X > gutterWidth)
        {
            // Mouse is outside gutter
            if (_hoverLine != -1)
            {
                _hoverLine = -1;
                _codeEdit.QueueRedraw();
            }
            return;
        }

        // Use GetLineColumnAtPos which properly handles scroll, line height, and scale
        var mousePosI = new Vector2I((int)mousePos.X, (int)mousePos.Y);
        var lineCol = _codeEdit.GetLineColumnAtPos(mousePosI);
        int line = lineCol.Y;

        if (line < 0 || line >= _codeEdit.GetLineCount())
        {
            if (_hoverLine != -1)
            {
                _hoverLine = -1;
                _codeEdit.QueueRedraw();
            }
            return;
        }

        lock (_referencesLock)
        {
            if (!_references.ContainsKey(line))
            {
                if (_hoverLine != -1)
                {
                    _hoverLine = -1;
                    _codeEdit.QueueRedraw();
                }
                return;
            }
        }

        if (_hoverLine != line)
        {
            _hoverLine = line;
            _codeEdit.QueueRedraw();
        }
    }

    #endregion

    #region Click Handling

    private void OnGutterClicked(long line, long gutterId)
    {
        int lineInt = (int)line;
        int gutterIdInt = (int)gutterId;

        if (gutterIdInt == _refCountGutterId)
        {
            ReferenceInfo info;
            lock (_referencesLock)
            {
                if (!_references.TryGetValue(lineInt, out info))
                    return;
            }

            var gutterWidth = _codeEdit?.GetGutterWidth(_refCountGutterId) ?? 60;
            var iconZoneStart = gutterWidth - IconZoneWidth;
            bool hasRefs = info.StrictCount > 0 || info.PotentialCount > 0;

            if (_lastMouseX >= iconZoneStart)
            {
                // Click on info icon → open TypeInferencePanel
                Logger.Info($"GDGutterManager: Info icon clicked at line {lineInt}, symbol={info.SymbolName}");
                TypeClicked?.Invoke(info.SymbolName, lineInt, _scriptFile);
            }
            else if (hasRefs)
            {
                // Click on reference count → Find References
                Logger.Info($"GDGutterManager: References clicked at line {lineInt}, symbol={info.SymbolName}");
                ReferencesClicked?.Invoke(info.SymbolName, lineInt);
            }
        }
        else if (gutterIdInt == _errorGutterId)
        {
            lock (_diagnosticsLock)
            {
                if (_diagnostics.TryGetValue(lineInt, out var info))
                {
                    Logger.Info($"GDGutterManager: Error gutter clicked at line {lineInt}, code={info.Code}");
                    DiagnosticClicked?.Invoke(lineInt, info.Code);
                }
            }
        }
    }

    #endregion

    #region Line Backgrounds

    private void UpdateLineBackgrounds()
    {
        if (_codeEdit == null)
            return;

        // First clear all backgrounds
        ClearLineBackgrounds();

        if (!_errorLineBackgroundEnabled)
            return;

        // Set background for lines with errors/warnings
        lock (_diagnosticsLock)
        {
            foreach (var kvp in _diagnostics)
            {
                var bgColor = kvp.Value.Severity switch
                {
                    GDDiagnosticSeverity.Error => ErrorBgColor,
                    GDDiagnosticSeverity.Warning => WarningBgColor,
                    _ => Colors.Transparent
                };

                if (bgColor != Colors.Transparent)
                {
                    _codeEdit.SetLineBackgroundColor(kvp.Key, bgColor);
                }
            }
        }
    }

    private void ClearLineBackgrounds()
    {
        if (_codeEdit == null)
            return;

        for (int i = 0; i < _codeEdit.GetLineCount(); i++)
        {
            _codeEdit.SetLineBackgroundColor(i, Colors.Transparent);
        }
    }

    #endregion

    #region Reference Counting

    private void RefreshReferenceCounts()
    {
        // Cancel any pending type inference
        _typeInferenceCts?.Cancel();
        _typeInferenceCts?.Dispose();
        _typeInferenceCts = new CancellationTokenSource();

        lock (_referencesLock)
        {
            _references.Clear();
        }

        if (_scriptFile?.Class == null || _scriptProject == null)
        {
            Logger.Debug("GDGutterManager: No script or project, skipping refresh");
            return;
        }

        // Find all declarations in the current script
        var declarations = FindDeclarations(_scriptFile.Class);
        Logger.Debug($"GDGutterManager: Found {declarations.Count} declarations");

        // Compute reference counts for each declaration
        foreach (var decl in declarations)
        {
            if (string.IsNullOrEmpty(decl.Name))
                continue;

            var (strictCount, potentialCount) = CountReferencesWithSemantics(decl);

            // Subtract 1 from strictCount to exclude the declaration itself
            int displayStrictCount = Math.Max(0, strictCount - 1);

            // Always add entry for declarations (for info icon)
            lock (_referencesLock)
            {
                _references[decl.Line] = new ReferenceInfo
                {
                    SymbolName = decl.Name,
                    StrictCount = displayStrictCount,
                    PotentialCount = potentialCount,
                    Line = decl.Line
                };
            }
        }

        Logger.Debug($"GDGutterManager: {_references.Count} declarations");

        // Request redraw
        _codeEdit?.QueueRedraw();
    }

    private List<DeclarationInfo> FindDeclarations(GDClassDeclaration classDecl)
    {
        var declarations = new List<DeclarationInfo>();

        foreach (var token in classDecl.AllTokens)
        {
            if (token is not GDIdentifier identifier)
                continue;

            var parent = identifier.Parent;

            // Check if it's a declaration
            bool isDeclaration = parent switch
            {
                GDMethodDeclaration => true,
                GDVariableDeclaration => true,
                GDVariableDeclarationStatement => true,  // Local variables in methods
                GDSignalDeclaration => true,
                GDParameterDeclaration => true,
                GDEnumDeclaration => true,
                GDInnerClassDeclaration => true,
                GDForStatement => true,  // For loop variable
                _ => false
            };

            if (isDeclaration)
            {
                declarations.Add(new DeclarationInfo
                {
                    Name = identifier.Sequence,
                    Line = identifier.StartLine,
                    Column = identifier.StartColumn,
                    Identifier = identifier,
                    ParentNode = parent
                });
            }
        }

        // Deduplicate by line
        return declarations
            .GroupBy(d => d.Line)
            .Select(g => g.First())
            .ToList();
    }

    private (int strictCount, int potentialCount) CountReferencesWithSemantics(DeclarationInfo decl)
    {
        if (_scriptFile?.Class == null || decl.Identifier == null)
        {
            return (CountReferencesSimple(decl.Name), 0);
        }

        // Create context for refactoring service
        var reference = new GDScriptReference(_scriptFile.FullPath ?? "unknown.gd");
        var scriptFile = new GDScriptFile(reference);
        scriptFile.Reload(_scriptFile.Class.ToString());

        var cursor = new GDCursorPosition(decl.Line, decl.Column);
        var selection = GDSelectionInfo.None;

        var context = new GDRefactoringContext(
            scriptFile,
            _scriptFile.Class,
            cursor,
            selection);

        // Determine symbol scope
        var scope = _findReferencesService.DetermineSymbolScope(decl.Identifier, context);
        if (scope == null)
        {
            return (CountReferencesSimple(decl.Name), 0);
        }

        // For local/parameter/for-loop scope
        if (scope.Type == GDSymbolScopeType.LocalVariable ||
            scope.Type == GDSymbolScopeType.MethodParameter ||
            scope.Type == GDSymbolScopeType.ForLoopVariable ||
            scope.Type == GDSymbolScopeType.MatchCaseVariable)
        {
            var result = _findReferencesService.FindReferencesForScope(context, scope);
            return (result.StrictReferences.Count, result.PotentialReferences.Count);
        }

        // For class members - use cross-file search
        if (scope.Type == GDSymbolScopeType.ClassMember)
        {
            return CountClassMemberReferences(scope, decl.Name);
        }

        return (CountReferencesSimple(decl.Name), 0);
    }

    private (int strictCount, int potentialCount) CountClassMemberReferences(GDSymbolScope scope, string symbolName)
    {
        int strictCount = 0;
        int potentialCount = 0;

        // Count in current file
        if (scope.ContainingClass != null)
        {
            strictCount += scope.ContainingClass.AllTokens.OfType<GDIdentifier>()
                .Count(i => i.Sequence == symbolName);
        }

        // For public members, also count cross-file references
        if (scope.IsPublic && !string.IsNullOrEmpty(_scriptFile?.TypeName) && _scriptProject != null)
        {
            var typeName = _scriptFile.TypeName;

            foreach (var script in _scriptProject.ScriptFiles)
            {
                if (script == _scriptFile || script.Class == null)
                    continue;

                var analyzer = script.Analyzer;

                foreach (var memberOp in script.Class.AllNodes.OfType<GDMemberOperatorExpression>())
                {
                    if (memberOp.Identifier?.Sequence != symbolName)
                        continue;

                    if (analyzer != null)
                    {
                        var callerType = analyzer.GetTypeForNode(memberOp.CallerExpression);
                        if (callerType == typeName)
                        {
                            strictCount++;
                        }
                        else if (callerType == "Variant" || string.IsNullOrEmpty(callerType))
                        {
                            potentialCount++;
                        }
                    }
                    else
                    {
                        potentialCount++;
                    }
                }
            }
        }

        return (strictCount, potentialCount);
    }

    private int CountReferencesSimple(string symbolName)
    {
        if (_scriptProject == null)
            return 0;

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

    #endregion

    #region Helpers

    private static int GetSeverityPriority(GDDiagnosticSeverity severity)
    {
        return severity switch
        {
            GDDiagnosticSeverity.Error => 3,
            GDDiagnosticSeverity.Warning => 2,
            GDDiagnosticSeverity.Hint => 1,
            _ => 0
        };
    }

    #endregion

    #region Data Types

    private struct DeclarationInfo
    {
        public string Name;
        public int Line;
        public int Column;
        public GDIdentifier? Identifier;
        public GDNode? ParentNode;
    }

    private struct ReferenceInfo
    {
        public string SymbolName;
        public int StrictCount;
        public int PotentialCount;
        public int Line;
    }

    private struct DiagnosticDisplayInfo
    {
        public string Code;
        public string Message;
        public GDDiagnosticSeverity Severity;
        public int Line;
        public int Count;
    }

    #endregion
}
