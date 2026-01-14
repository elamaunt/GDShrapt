using Godot;
using GDShrapt.Reader;
using GDShrapt.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Overlay control that displays reference counts above declarations in the editor.
/// Positioned over the TextEdit control and synchronized with scroll.
/// </summary>
internal partial class ReferenceCountOverlay : Control
{
    private TextEdit _textEdit;
    private GDScriptProject _scriptProject;
    private GDScriptFile _ScriptFile;

    private readonly Dictionary<int, ReferenceCountInfo> _referenceCountCache = new();
    private readonly GDFindReferencesService _findReferencesService = new();
    private bool _needsRefresh = true;
    private double _refreshTimer = 0;
    private const double RefreshDelay = 1.0; // Debounce delay in seconds

    /// <summary>
    /// Event fired when user clicks on a reference count to see details.
    /// Parameters: symbolName, line
    /// </summary>
    public event Action<string, int> ReferenceCountClicked;

    public override void _Ready()
    {
        // Transparent background - we only draw text
        MouseFilter = MouseFilterEnum.Pass;
    }

    /// <summary>
    /// Attaches the overlay to a TextEdit control.
    /// </summary>
    public void AttachToEditor(TextEdit textEdit, GDScriptProject ScriptProject)
    {
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

            // We need to redraw when editor scrolls
            // TextEdit doesn't have a direct scroll signal, so we update in _Process

            _needsRefresh = true;
        }
    }

    /// <summary>
    /// Sets the current script being edited.
    /// </summary>
    public void SetScript(GDScriptFile ScriptFile)
    {
        _ScriptFile = ScriptFile;
        _needsRefresh = true;
        QueueRedraw();
    }

    /// <summary>
    /// Detaches from the current TextEdit.
    /// </summary>
    public void Detach()
    {
        if (_textEdit != null)
        {
            _textEdit.TextChanged -= OnTextChanged;
            _textEdit = null;
        }

        _ScriptFile = null;
        _referenceCountCache.Clear();
        QueueRedraw();
    }

    private void OnTextChanged()
    {
        _needsRefresh = true;
        _refreshTimer = RefreshDelay;
    }

    public override void _Process(double delta)
    {
        if (_textEdit == null || !IsVisibleInTree())
            return;

        // Debounced refresh
        if (_refreshTimer > 0)
        {
            _refreshTimer -= delta;
            if (_refreshTimer <= 0)
            {
                RefreshReferenceCounts();
            }
        }

        // Always redraw to handle scroll changes
        QueueRedraw();
    }

    private void RefreshReferenceCounts()
    {
        _referenceCountCache.Clear();

        if (_ScriptFile?.Class == null || _scriptProject == null)
            return;

        // Find all declarations in the current script
        var declarations = FindDeclarations(_ScriptFile.Class);

        foreach (var decl in declarations)
        {
            if (string.IsNullOrEmpty(decl.Name))
                continue;

            // Count references using semantic engine for accurate scope-aware counting
            int count = CountReferencesWithSemantics(decl);

            if (count > 0)
            {
                _referenceCountCache[decl.Line] = new ReferenceCountInfo
                {
                    SymbolName = decl.Name,
                    Count = count,
                    Line = decl.Line
                };
            }
        }

        _needsRefresh = false;
        QueueRedraw();
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
                GDMethodDeclaration _ => true,
                GDVariableDeclaration _ => true,
                GDSignalDeclaration _ => true,
                GDParameterDeclaration _ => true,
                GDEnumDeclaration _ => true,
                GDInnerClassDeclaration _ => true,
                _ => false
            };

            if (isDeclaration)
            {
                declarations.Add(new DeclarationInfo
                {
                    Name = identifier.Sequence,
                    Line = identifier.StartLine,
                    Column = identifier.StartColumn,
                    Identifier = identifier
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
    /// </summary>
    private int CountReferencesWithSemantics(DeclarationInfo decl)
    {
        if (_ScriptFile?.Class == null || decl.Identifier == null)
            return CountReferencesSimple(decl.Name);

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
            return CountReferencesSimple(decl.Name);

        // For local/parameter/for-loop scope - count only within their scope
        if (scope.Type == GDSymbolScopeType.LocalVariable ||
            scope.Type == GDSymbolScopeType.MethodParameter ||
            scope.Type == GDSymbolScopeType.ForLoopVariable ||
            scope.Type == GDSymbolScopeType.MatchCaseVariable)
        {
            var result = _findReferencesService.FindReferencesForScope(context, scope);
            return result.TotalCount;
        }

        // For class members - use cross-file search with type awareness
        if (scope.Type == GDSymbolScopeType.ClassMember)
        {
            return CountClassMemberReferences(scope, decl.Name);
        }

        // Fallback for project-wide symbols
        return CountReferencesSimple(decl.Name);
    }

    /// <summary>
    /// Counts references for a class member, including cross-file references with type inference.
    /// </summary>
    private int CountClassMemberReferences(GDSymbolScope scope, string symbolName)
    {
        int count = 0;

        // Count in current file (all identifiers with this name)
        if (scope.ContainingClass != null)
        {
            count += scope.ContainingClass.AllTokens.OfType<GDIdentifier>()
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
                            count++;
                        }
                    }
                }
            }
        }

        return count;
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

    public override void _Draw()
    {
        if (_textEdit == null || _referenceCountCache.Count == 0)
            return;

        var font = ThemeDB.FallbackFont;
        var fontSize = 11;
        var textColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Gray, semi-transparent
        var hoverColor = new Color(0.3f, 0.6f, 1.0f, 0.9f); // Blue when hoverable

        // Get visible line range
        int firstVisibleLine = _textEdit.GetFirstVisibleLine();
        int lastVisibleLine = firstVisibleLine + _textEdit.GetVisibleLineCount() + 1;

        // Get the line height
        float lineHeight = _textEdit.GetLineHeight();

        foreach (var kvp in _referenceCountCache)
        {
            int line = kvp.Key;

            // Skip if not visible
            if (line < firstVisibleLine || line > lastVisibleLine)
                continue;

            var info = kvp.Value;

            // Calculate position - above the line
            float yPos = (line - firstVisibleLine) * lineHeight - 2;

            // Offset for the gutter/line numbers (estimate)
            float xPos = 60;

            // Format: "N references" or "1 reference"
            string text = info.Count == 1 ? "1 reference" : $"{info.Count} references";

            // Check if mouse is near this text for hover effect
            var mousePos = GetLocalMousePosition();
            var textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
            var textRect = new Rect2(xPos, yPos - fontSize, textSize.X, fontSize + 4);

            Color drawColor = textRect.HasPoint(mousePos) ? hoverColor : textColor;

            // Draw the reference count text
            DrawString(font, new Vector2(xPos, yPos), text, HorizontalAlignment.Left, -1, fontSize, drawColor);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton &&
            mouseButton.ButtonIndex == MouseButton.Left &&
            mouseButton.Pressed)
        {
            // Check if clicked on any reference count
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

                string text = info.Count == 1 ? "1 reference" : $"{info.Count} references";
                var textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
                var textRect = new Rect2(xPos, yPos - fontSize, textSize.X, fontSize + 4);

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
    }

    private struct ReferenceCountInfo
    {
        public string SymbolName;
        public int Count;
        public int Line;
    }
}
