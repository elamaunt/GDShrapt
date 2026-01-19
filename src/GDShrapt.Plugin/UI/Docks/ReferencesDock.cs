using Godot;
using GDShrapt.Reader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Plugin;

/// <summary>
/// Dock panel for displaying find references results.
/// Shows references grouped by file with navigation support.
/// </summary>
internal partial class ReferencesDock : Control
{
    private Label _headerLabel;
    private StyledReferencesTree _referencesTree;

    private string _currentSymbol;
    private List<ReferenceItem> _references = new();

    /// <summary>
    /// Event fired when user wants to navigate to a reference.
    /// Parameters: filePath, line, startColumn, endColumn
    /// </summary>
    public event Action<string, int, int, int> NavigateToReference;

    public override void _Ready()
    {
        Logger.Info("ReferencesDock._Ready() called");
        EnsureUICreated();
    }

    /// <summary>
    /// Initializes the dock. Call this after creation to ensure UI is ready.
    /// </summary>
    public void Initialize()
    {
        Logger.Info("ReferencesDock.Initialize() called");
        EnsureUICreated();
    }

    private void EnsureUICreated()
    {
        if (_referencesTree != null)
            return;

        CreateUI();
    }

    private void CreateUI()
    {
        Logger.Info($"ReferencesDock.CreateUI() called, GetChildCount={GetChildCount()}");

        // Main container
        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        mainVBox.AddThemeConstantOverride("separation", 4);
        AddChild(mainVBox);

        // Header
        _headerLabel = new Label
        {
            Text = LocalizationManager.Tr(Strings.MenuFindReferences),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _headerLabel.AddThemeFontSizeOverride("font_size", 14);
        mainVBox.AddChild(_headerLabel);

        // Separator
        mainVBox.AddChild(new HSeparator());

        // Styled tree with references and syntax highlighting
        _referencesTree = new StyledReferencesTree
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _referencesTree.ItemActivated += OnTreeItemActivated;
        _referencesTree.ItemSelected += OnTreeItemSelected;
        mainVBox.AddChild(_referencesTree);

        // Set minimum size
        CustomMinimumSize = new Vector2(250, 150);
    }

    /// <summary>
    /// Shows references for the given symbol.
    /// </summary>
    internal void ShowReferences(string symbolName, IEnumerable<GDMemberReference> references)
    {
        _currentSymbol = symbolName;
        _references.Clear();

        foreach (var reference in references)
        {
            var item = ReferenceItem.FromGDMemberReference(reference);
            // Recalculate highlight with explicit symbolName for precision
            CalculateHighlightPosition(item, symbolName);
            _references.Add(item);
        }

        UpdateDisplay();
        Show();
    }

    /// <summary>
    /// Calculates highlight position for the symbol within the context line.
    /// </summary>
    private static void CalculateHighlightPosition(ReferenceItem item, string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName) || string.IsNullOrEmpty(item.ContextLine))
            return;

        var idx = item.ContextLine.IndexOf(symbolName, StringComparison.Ordinal);
        if (idx >= 0)
        {
            item.HighlightStart = idx;
            item.HighlightEnd = idx + symbolName.Length;
        }
    }

    /// <summary>
    /// Shows references from a list of reference items.
    /// </summary>
    public void ShowReferences(string symbolName, IEnumerable<ReferenceItem> items)
    {
        _currentSymbol = symbolName;
        _references = items.ToList();

        UpdateDisplay();
        Show();
    }

    /// <summary>
    /// Clears the references display.
    /// </summary>
    public void ClearReferences()
    {
        _currentSymbol = null;
        _references.Clear();

        if (_referencesTree != null)
            _referencesTree.Clear();
        if (_headerLabel != null)
            _headerLabel.Text = LocalizationManager.Tr(Strings.MenuFindReferences);
    }

    private void UpdateDisplay()
    {
        EnsureUICreated();
        _referencesTree.Clear();

        if (string.IsNullOrEmpty(_currentSymbol) || _references.Count == 0)
        {
            _headerLabel.Text = LocalizationManager.Tr(Strings.MenuFindReferences);
            return;
        }

        _headerLabel.Text = $"\"{_currentSymbol}\" ({_references.Count} references)";

        var root = _referencesTree.CreateItem();

        // Group references by file
        var groupedByFile = _references
            .GroupBy(r => r.FilePath ?? "Unknown")
            .OrderBy(g => g.Key);

        foreach (var fileGroup in groupedByFile)
        {
            // Create file header using styled tree
            var fileItem = _referencesTree.CreateFileItem(root, GetFileName(fileGroup.Key), fileGroup.Count(), fileGroup.Key);

            // Create reference items with syntax highlighting
            foreach (var reference in fileGroup.OrderBy(r => r.Line))
            {
                _referencesTree.CreateReferenceItem(fileItem, reference);
            }

            // Expand by default
            fileItem.Collapsed = false;
        }
    }

    private static string GetFileName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Unknown";

        var lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
    }

    private void OnTreeItemActivated()
    {
        var selected = _referencesTree.GetSelected();
        if (selected == null)
            return;

        // Try to get ReferenceItem from tree's internal mapping first
        var refFromTree = _referencesTree.GetReferenceForItem(selected);
        if (refFromTree != null)
        {
            NavigateToReference?.Invoke(refFromTree.FilePath, refFromTree.Line, refFromTree.Column, refFromTree.EndColumn);
            return;
        }

        // Try to get from metadata
        var metadata = selected.GetMetadata(0);
        if (metadata.VariantType != Variant.Type.Object)
            return;

        // Check if it's a MetadataContainer
        var container = metadata.As<MetadataContainer>();
        if (container == null)
            return;

        // Try to get ReferenceItem from first metadata slot
        var firstMeta = container.GetMetadata(0);
        if (firstMeta.VariantType == Variant.Type.Object)
        {
            var obj = firstMeta.AsGodotObject();
            if (obj is ReferenceItem refItem)
            {
                NavigateToReference?.Invoke(refItem.FilePath, refItem.Line, refItem.Column, refItem.EndColumn);
                return;
            }
            // If it's HeaderLayoutData, this is a file header - skip to second metadata
        }

        // Could be a file header - try second metadata slot for file path
        var secondMeta = container.GetMetadata(1);
        if (secondMeta.VariantType == Variant.Type.String)
        {
            var filePath = secondMeta.AsString();
            if (!string.IsNullOrEmpty(filePath))
            {
                var firstRef = _references.FirstOrDefault(r => r.FilePath == filePath);
                if (firstRef != null)
                {
                    NavigateToReference?.Invoke(firstRef.FilePath, firstRef.Line, firstRef.Column, firstRef.EndColumn);
                }
            }
        }
    }

    private void OnTreeItemSelected()
    {
        // Navigate on single click (same logic as double click)
        OnTreeItemActivated();
    }
}

/// <summary>
/// Represents a single reference to a symbol.
/// </summary>
internal partial class ReferenceItem : GodotObject
{
    public string FilePath { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    /// <summary>
    /// End column of the token for precise selection.
    /// </summary>
    public int EndColumn { get; set; }
    public string ContextLine { get; set; }
    public GDPluginReferenceKind Kind { get; set; }
    public GDIdentifier Identifier { get; set; }

    /// <summary>
    /// Start index of the symbol within ContextLine for highlighting.
    /// </summary>
    public int HighlightStart { get; set; }

    /// <summary>
    /// End index of the symbol within ContextLine for highlighting.
    /// </summary>
    public int HighlightEnd { get; set; }

    /// <summary>
    /// Confidence level of the reference.
    /// </summary>
    public GDReferenceConfidence Confidence { get; set; } = GDReferenceConfidence.Strict;

    public ReferenceItem() { }

    public ReferenceItem(string filePath, int line, int column, int endColumn, string context, GDPluginReferenceKind kind, int highlightStart = 0, int highlightEnd = 0)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
        EndColumn = endColumn;
        ContextLine = context;
        Kind = kind;
        HighlightStart = highlightStart;
        HighlightEnd = highlightEnd > 0 ? highlightEnd : context?.Length ?? 0;
    }

    internal static ReferenceItem FromGDMemberReference(GDMemberReference memberRef)
    {
        var contextLine = GetContextLine(memberRef);
        var symbolName = memberRef.Identifier?.Sequence;

        // Calculate highlight position
        int highlightStart = 0;
        int highlightEnd = 0;

        if (!string.IsNullOrEmpty(symbolName) && !string.IsNullOrEmpty(contextLine))
        {
            var idx = contextLine.IndexOf(symbolName, StringComparison.Ordinal);
            if (idx >= 0)
            {
                highlightStart = idx;
                highlightEnd = idx + symbolName.Length;
            }
        }

        return new ReferenceItem
        {
            Identifier = memberRef.Identifier,
            FilePath = memberRef.Script?.FullPath,
            Line = memberRef.Identifier?.StartLine ?? 0,
            Column = memberRef.Identifier?.StartColumn ?? 0,
            EndColumn = memberRef.Identifier?.EndColumn ?? 0,
            Kind = DetermineKind(memberRef),
            ContextLine = contextLine,
            HighlightStart = highlightStart,
            HighlightEnd = highlightEnd,
            Confidence = memberRef.Confidence
        };
    }

    private static GDPluginReferenceKind DetermineKind(GDMemberReference memberRef)
    {
        if (memberRef.Member != null)
            return GDPluginReferenceKind.Declaration;

        // Check if it's a call based on parent node
        var parent = memberRef.Identifier?.Parent;
        if (parent is GDCallExpression)
            return GDPluginReferenceKind.Call;
        if (parent is GDIdentifierExpression idExpr && idExpr.Parent is GDCallExpression)
            return GDPluginReferenceKind.Call;

        return GDPluginReferenceKind.Read;
    }

    private static string GetContextLine(GDMemberReference memberRef)
    {
        // Get the line containing the identifier for context
        if (memberRef.Identifier == null)
            return "";

        var identifier = memberRef.Identifier;
        var symbolName = identifier.Sequence;
        var parent = identifier.Parent;

        // Special handling for parameter declarations
        if (parent is GDParameterDeclaration)
        {
            return $"param {symbolName}";
        }

        // Special handling for variable declarations (class-level)
        if (parent is GDVariableDeclaration)
        {
            return $"var {symbolName}";
        }

        // Special handling for local variable declarations
        if (parent is GDVariableDeclarationStatement)
        {
            return $"var {symbolName}";
        }

        // Special handling for method declarations
        if (parent is GDMethodDeclaration methodDecl)
        {
            return $"func {methodDecl.Identifier?.Sequence ?? symbolName}(...)";
        }

        // Special handling for signal declarations
        if (parent is GDSignalDeclaration)
        {
            return $"signal {symbolName}";
        }

        // Special handling for enum declarations
        if (parent is GDEnumDeclaration)
        {
            return $"enum {symbolName}";
        }

        // Special handling for inner class declarations
        if (parent is GDInnerClassDeclaration)
        {
            return $"class {symbolName}";
        }

        // For expressions - walk up to find statement context
        var current = parent;
        while (current != null && !(current is GDStatement) && !(current is GDClassMember))
        {
            current = current.Parent;
        }

        if (current != null && !(current is GDMethodDeclaration))
        {
            var text = current.ToString();
            // Limit length
            if (text.Length > 60)
                text = text.Substring(0, 57) + "...";
            return text.Trim().Replace("\n", " ").Replace("\r", "");
        }

        return symbolName;
    }
}

/// <summary>
/// Kind of reference (how the symbol is used).
/// </summary>
internal enum GDPluginReferenceKind
{
    Read,
    Write,
    Call,
    Declaration
}
