using Godot;
using GDShrapt.Plugin.Localization;
using System;
using System.Collections.Generic;

namespace GDShrapt.Plugin.UI;

/// <summary>
/// Dock panel for viewing GDShrapt API documentation.
/// Allows other plugin developers to learn how to use the public API.
/// </summary>
internal partial class ApiDocumentationDock : Control
{
    private LineEdit _searchField;
    private Tree _navigationTree;
    private RichTextLabel _contentView;
    private Label _titleLabel;

    private readonly Dictionary<string, string> _documentation = new();
    private readonly Dictionary<string, TreeItem> _treeItems = new();

    public override void _Ready()
    {
        Name = "API Documentation";
        LoadDocumentation();
        CreateUI();
        PopulateTree();
    }

    private void CreateUI()
    {
        // Main container
        var mainVBox = new VBoxContainer();
        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        mainVBox.AddThemeConstantOverride("separation", 4);
        AddChild(mainVBox);

        // Title
        _titleLabel = new Label
        {
            Text = "GDShrapt API Documentation",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        mainVBox.AddChild(_titleLabel);

        // Separator
        mainVBox.AddChild(new HSeparator());

        // Search field
        _searchField = new LineEdit
        {
            PlaceholderText = "Search API...",
            ClearButtonEnabled = true
        };
        _searchField.TextChanged += OnSearchChanged;
        mainVBox.AddChild(_searchField);

        // Split container for navigation and content
        var splitContainer = new HSplitContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SplitOffset = 180
        };
        mainVBox.AddChild(splitContainer);

        // Left panel: Navigation tree
        _navigationTree = new Tree
        {
            HideRoot = true,
            SelectMode = Tree.SelectModeEnum.Single,
            CustomMinimumSize = new Vector2(150, 0),
            SizeFlagsHorizontal = SizeFlags.Fill
        };
        _navigationTree.ItemSelected += OnTreeItemSelected;
        splitContainer.AddChild(_navigationTree);

        // Right panel: Content view with scroll
        var scrollContainer = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        splitContainer.AddChild(scrollContainer);

        _contentView = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            SelectionEnabled = true,
            ContextMenuEnabled = true,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        _contentView.AddThemeColorOverride("default_color", new Color(0.9f, 0.9f, 0.9f));
        _contentView.MetaClicked += OnMetaClicked;
        scrollContainer.AddChild(_contentView);

        // Set minimum size
        CustomMinimumSize = new Vector2(400, 300);
    }

    private void PopulateTree()
    {
        _navigationTree.Clear();
        _treeItems.Clear();

        var root = _navigationTree.CreateItem();

        // Main API section
        var apiSection = CreateSection(root, "Getting Started");
        CreateDocItem(apiSection, "Overview", "overview");
        CreateDocItem(apiSection, "GDShraptApi", "gdshrapt_api");
        CreateDocItem(apiSection, "IGDShraptServices", "services");

        // Analysis section
        var analysisSection = CreateSection(root, "Analysis");
        CreateDocItem(analysisSection, "IProjectAnalyzer", "project_analyzer");
        CreateDocItem(analysisSection, "IScriptInfo", "script_info");
        CreateDocItem(analysisSection, "ISymbolInfo", "symbol_info");
        CreateDocItem(analysisSection, "IIdentifierInfo", "identifier_info");

        // References section
        var refsSection = CreateSection(root, "References");
        CreateDocItem(refsSection, "IReferenceFinder", "reference_finder");
        CreateDocItem(refsSection, "IReferenceInfo", "reference_info");

        // Navigation section
        var navSection = CreateSection(root, "Navigation");
        CreateDocItem(navSection, "ICodeNavigator", "code_navigator");
        CreateDocItem(navSection, "ITypeResolver", "type_resolver");
        CreateDocItem(navSection, "ITypeInfo", "type_info");

        // Modification section
        var modSection = CreateSection(root, "Code Modification");
        CreateDocItem(modSection, "ICodeModifier", "code_modifier");
        CreateDocItem(modSection, "IRenameResult", "rename_result");

        // Examples section
        var examplesSection = CreateSection(root, "Examples");
        CreateDocItem(examplesSection, "Basic Usage", "example_basic");
        CreateDocItem(examplesSection, "Find References", "example_find_refs");
        CreateDocItem(examplesSection, "Rename Symbol", "example_rename");

        // Select first item to show content by default
        if (_treeItems.TryGetValue("overview", out var overviewItem))
        {
            overviewItem.Select(0);
            ShowDocumentation("overview");
        }
    }

    private TreeItem CreateSection(TreeItem parent, string title)
    {
        var item = _navigationTree.CreateItem(parent);
        item.SetText(0, title);
        item.SetSelectable(0, false);
        item.SetCustomColor(0, new Color(0.7f, 0.7f, 0.7f));
        item.Collapsed = false;
        return item;
    }

    private TreeItem CreateDocItem(TreeItem parent, string title, string docKey)
    {
        var item = _navigationTree.CreateItem(parent);
        item.SetText(0, title);
        item.SetMetadata(0, docKey);
        _treeItems[docKey] = item;
        return item;
    }

    private void OnSearchChanged(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
        {
            // Show all items
            foreach (var kvp in _treeItems)
            {
                kvp.Value.Visible = true;
            }
            return;
        }

        var searchLower = newText.ToLowerInvariant();
        foreach (var kvp in _treeItems)
        {
            var matches = kvp.Value.GetText(0).ToLowerInvariant().Contains(searchLower) ||
                         (_documentation.TryGetValue(kvp.Key, out var doc) &&
                          doc.ToLowerInvariant().Contains(searchLower));
            kvp.Value.Visible = matches;
        }
    }

    private void OnTreeItemSelected()
    {
        var selected = _navigationTree.GetSelected();
        if (selected == null)
            return;

        var metadata = selected.GetMetadata(0);
        if (metadata.VariantType == Variant.Type.String)
        {
            var docKey = metadata.AsString();
            ShowDocumentation(docKey);
        }
    }

    private void ShowDocumentation(string docKey)
    {
        _contentView.Clear();

        if (_documentation.TryGetValue(docKey, out var content))
        {
            // Must use ParseBbcode or set Text to render BBCode
            // Text property works when BbcodeEnabled = true
            _contentView.Text = content;
            Logger.Debug($"API Doc: Showing '{docKey}' ({content.Length} chars)");
        }
        else
        {
            _contentView.Text = "[color=gray]Documentation not found for: " + docKey + "[/color]";
            Logger.Debug($"API Doc: Documentation not found for '{docKey}'");
        }
    }

    private void OnMetaClicked(Variant meta)
    {
        var link = meta.AsString();
        if (link.StartsWith("doc:"))
        {
            var docKey = link.Substring(4);
            if (_treeItems.TryGetValue(docKey, out var item))
            {
                item.Select(0);
                ShowDocumentation(docKey);
            }
        }
        else if (link.StartsWith("http"))
        {
            OS.ShellOpen(link);
        }
    }

    private void LoadDocumentation()
    {
        // Overview
        _documentation["overview"] = @"[font_size=18][b]GDShrapt Public API[/b][/font_size]

[color=#aaaaaa]Version 1.0.0[/color]

The GDShrapt plugin exposes a public API that allows other Godot plugins to access GDScript analysis functionality.

[b]Features:[/b]
• Project-wide script analysis
• Reference finding across all scripts
• Type resolution
• Code navigation (go to definition)
• Code modification (rename, extract method)

[b]Quick Start:[/b]
1. Reference GDShrapt.Plugin via NuGet
2. Access the API through [url=doc:gdshrapt_api]GDShraptApi[/url] static class
3. Use [url=doc:services]IGDShraptServices[/url] to access functionality

[color=#88cc88]See the Examples section for code samples.[/color]";

        // GDShraptApi
        _documentation["gdshrapt_api"] = @"[font_size=18][b]GDShraptApi[/b][/font_size]

[color=#aaaaaa]Static entry point for accessing GDShrapt plugin functionality.[/color]

[b]Namespace:[/b] GDShrapt.Plugin.Api

[b]Properties:[/b]

[code]public static IGDShraptServices? Services { get; }[/code]
Gets the current instance of GDShrapt services.
Returns [color=yellow]null[/color] if the plugin is not initialized.

[code]public static bool IsInitialized { get; }[/code]
Checks if the plugin is initialized and ready to use.

[b]Events:[/b]

[code]public static event Action? Initialized;[/code]
Fired when the plugin is initialized. Subscribe to this if your plugin loads before GDShrapt.

[code]public static event Action? Disposing;[/code]
Fired when the plugin is being disposed. Clean up any references here.

[b]Usage:[/b]
[code]using GDShrapt.Plugin.Api;

if (GDShraptApi.IsInitialized)
{
    var services = GDShraptApi.Services;
    // Use services...
}
else
{
    GDShraptApi.Initialized += OnGDShraptReady;
}[/code]";

        // IGDShraptServices
        _documentation["services"] = @"[font_size=18][b]IGDShraptServices[/b][/font_size]

[color=#aaaaaa]Main interface for accessing all GDShrapt plugin services.[/color]

[b]Namespace:[/b] GDShrapt.Plugin.Api

[b]Properties:[/b]

[code]IProjectAnalyzer ProjectAnalyzer { get; }[/code]
Access to project-wide script analysis. See [url=doc:project_analyzer]IProjectAnalyzer[/url].

[code]IReferenceFinder ReferenceFinder { get; }[/code]
Find references to symbols. See [url=doc:reference_finder]IReferenceFinder[/url].

[code]ITypeResolver TypeResolver { get; }[/code]
Type resolution functionality. See [url=doc:type_resolver]ITypeResolver[/url].

[code]ICodeNavigator CodeNavigator { get; }[/code]
Navigate to definitions. See [url=doc:code_navigator]ICodeNavigator[/url].

[code]ICodeModifier CodeModifier { get; }[/code]
Code modification operations. See [url=doc:code_modifier]ICodeModifier[/url].

[code]Version ApiVersion { get; }[/code]
Plugin version for compatibility checks.";

        // IProjectAnalyzer
        _documentation["project_analyzer"] = @"[font_size=18][b]IProjectAnalyzer[/b][/font_size]

[color=#aaaaaa]Provides access to project-wide GDScript analysis.[/color]

[b]Properties:[/b]

[code]IReadOnlyList<IScriptInfo> Scripts { get; }[/code]
Gets all scripts currently loaded in the project.

[b]Methods:[/b]

[code]IScriptInfo? GetScriptByResourcePath(string resourcePath)[/code]
Gets a script by its Godot resource path (e.g., ""res://scripts/player.gd"").

[code]IScriptInfo? GetScriptByFullPath(string fullPath)[/code]
Gets a script by its full file system path.

[code]IScriptInfo? GetScriptByTypeName(string typeName)[/code]
Gets a script by its class_name (e.g., ""Player"").

[b]Events:[/b]

[code]event Action<IScriptInfo>? ScriptAdded;[/code]
Fired when a new script is added to the project.

[code]event Action<string>? ScriptRemoved;[/code]
Fired when a script is removed (parameter is resource path).

[code]event Action<IScriptInfo>? ScriptChanged;[/code]
Fired when a script is modified and reparsed.";

        // IScriptInfo
        _documentation["script_info"] = @"[font_size=18][b]IScriptInfo[/b][/font_size]

[color=#aaaaaa]Provides information about a single GDScript file.[/color]

[b]Properties:[/b]

[code]string FullPath { get; }[/code]
Full file system path to the script.

[code]string ResourcePath { get; }[/code]
Resource path (e.g., ""res://scripts/player.gd"").

[code]string? TypeName { get; }[/code]
Class/type name if defined with class_name.

[code]bool IsGlobal { get; }[/code]
Whether this is a global class (autoload or class_name).

[code]bool HasParseErrors { get; }[/code]
Whether there was an error parsing this script.

[code]GDClassDeclaration? AstRoot { get; }[/code]
The AST root node for advanced users who need direct AST access.

[b]Methods:[/b]

[code]IReadOnlyList<ISymbolInfo> GetDeclarations()[/code]
Gets all declarations (methods, variables, signals, etc.) in this script.

[code]IReadOnlyList<IIdentifierInfo> GetIdentifiers()[/code]
Gets all identifiers used in this script.";

        // ISymbolInfo
        _documentation["symbol_info"] = @"[font_size=18][b]ISymbolInfo[/b][/font_size]

[color=#aaaaaa]Information about a symbol declaration.[/color]

[b]Properties:[/b]

[code]string Name { get; }[/code]
Name of the symbol.

[code]SymbolKind Kind { get; }[/code]
Kind of symbol (Variable, Method, Signal, Constant, etc.).

[code]int Line { get; }[/code]
Line where declared (0-based).

[code]int Column { get; }[/code]
Column where declared (0-based).

[code]string? TypeAnnotation { get; }[/code]
Type annotation if present.

[code]string? Documentation { get; }[/code]
Documentation comment if present.

[b]SymbolKind Values:[/b]
• Variable
• Method
• Signal
• Constant
• Enum
• EnumValue
• Class
• InnerClass
• Parameter";

        // IIdentifierInfo
        _documentation["identifier_info"] = @"[font_size=18][b]IIdentifierInfo[/b][/font_size]

[color=#aaaaaa]Information about an identifier usage in code.[/color]

[b]Properties:[/b]

[code]string Name { get; }[/code]
The identifier name.

[code]int Line { get; }[/code]
Line number (0-based).

[code]int Column { get; }[/code]
Column number (0-based).

[code]string? InferredType { get; }[/code]
Inferred type if available.

[code]bool IsDeclaration { get; }[/code]
Whether this is a declaration.";

        // IReferenceFinder
        _documentation["reference_finder"] = @"[font_size=18][b]IReferenceFinder[/b][/font_size]

[color=#aaaaaa]Provides functionality for finding references across the project.[/color]

[b]Methods:[/b]

[code]Task<IReadOnlyList<IReferenceInfo>> FindReferencesAsync(
    string symbolName,
    CancellationToken cancellationToken = default)[/code]
Finds all references to a symbol by name across the entire project.

[code]Task<IReadOnlyList<IReferenceInfo>> FindReferencesAtAsync(
    string filePath,
    int line,
    int column,
    CancellationToken cancellationToken = default)[/code]
Finds all references to a symbol at a specific location.

[code]int CountReferences(string symbolName)[/code]
Quickly counts references to a symbol without retrieving details.

[code]IReadOnlyDictionary<string, int> GetReferenceCountsForScript(string filePath)[/code]
Gets reference counts for all declarations in a script.";

        // IReferenceInfo
        _documentation["reference_info"] = @"[font_size=18][b]IReferenceInfo[/b][/font_size]

[color=#aaaaaa]Information about a single reference to a symbol.[/color]

[b]Properties:[/b]

[code]string FilePath { get; }[/code]
Path to the file containing the reference.

[code]int Line { get; }[/code]
Line number (0-based).

[code]int Column { get; }[/code]
Column number (0-based).

[code]string ContextLine { get; }[/code]
Context line of code.

[code]ReferenceKind Kind { get; }[/code]
Kind of reference (Read, Write, Call, Declaration).

[code]GDIdentifier? Identifier { get; }[/code]
The identifier node from AST for advanced usage.";

        // ICodeNavigator
        _documentation["code_navigator"] = @"[font_size=18][b]ICodeNavigator[/b][/font_size]

[color=#aaaaaa]Provides code navigation functionality.[/color]

[b]Methods:[/b]

[code]Task<ILocationInfo?> FindDefinitionAsync(
    string filePath,
    int line,
    int column,
    CancellationToken cancellationToken = default)[/code]
Finds the definition of a symbol at the given location.

[code]Task<ILocationInfo?> FindDefinitionByNameAsync(
    string symbolName,
    CancellationToken cancellationToken = default)[/code]
Finds the definition of a symbol by name.

[b]ILocationInfo Properties:[/b]

[code]string FilePath { get; }[/code]
File path where the definition is located.

[code]int Line { get; }[/code]
Line number (0-based).

[code]int Column { get; }[/code]
Column number (0-based).";

        // ITypeResolver
        _documentation["type_resolver"] = @"[font_size=18][b]ITypeResolver[/b][/font_size]

[color=#aaaaaa]Provides type resolution functionality.[/color]

[b]Methods:[/b]

[code]Task<ITypeInfo?> GetTypeAtAsync(
    string filePath,
    int line,
    int column,
    CancellationToken cancellationToken = default)[/code]
Gets the inferred type for an identifier at a location.

[code]IReadOnlyList<ISymbolInfo> GetTypeMembers(string typeName)[/code]
Gets members of a type.

[code]bool TypeExists(string typeName)[/code]
Checks if a type exists in the project or Godot API.";

        // ITypeInfo
        _documentation["type_info"] = @"[font_size=18][b]ITypeInfo[/b][/font_size]

[color=#aaaaaa]Information about an inferred type.[/color]

[b]Properties:[/b]

[code]string Name { get; }[/code]
Type name.

[code]bool IsBuiltin { get; }[/code]
Whether this is a built-in Godot type.

[code]bool IsScriptType { get; }[/code]
Whether this is a script-defined type.

[code]string? BaseType { get; }[/code]
Base type if known.";

        // ICodeModifier
        _documentation["code_modifier"] = @"[font_size=18][b]ICodeModifier[/b][/font_size]

[color=#aaaaaa]Provides code modification functionality (rename, extract method).[/color]

[b]Methods:[/b]

[code]Task<IRenameResult> RenameAsync(
    string filePath,
    int line,
    int column,
    string newName,
    RenameOptions? options = null,
    CancellationToken cancellationToken = default)[/code]
Renames a symbol at the specified location across the entire project.

[code]Task<IExtractMethodResult> ExtractMethodAsync(
    string filePath,
    int startLine, int startColumn,
    int endLine, int endColumn,
    string methodName,
    CancellationToken cancellationToken = default)[/code]
Extracts selected code into a new method.

[code]Task<IReadOnlyList<ITextChange>> PreviewRenameAsync(
    string filePath,
    int line, int column,
    string newName,
    RenameOptions? options = null,
    CancellationToken cancellationToken = default)[/code]
Returns a preview of changes without applying them.

[b]RenameOptions:[/b]

[code]bool RenameOnlyStrongTyped { get; set; }[/code]
Only rename strongly-typed references.

[code]bool IncludeCommentsAndStrings { get; set; }[/code]
Include references in comments/strings.";

        // IRenameResult
        _documentation["rename_result"] = @"[font_size=18][b]IRenameResult[/b][/font_size]

[color=#aaaaaa]Result of a rename operation.[/color]

[b]Properties:[/b]

[code]bool Success { get; }[/code]
Whether the operation succeeded.

[code]string? ErrorMessage { get; }[/code]
Error message if failed.

[code]int FilesModified { get; }[/code]
Number of files modified.

[code]int ReferencesRenamed { get; }[/code]
Number of references renamed.

[code]IReadOnlyList<ITextChange> Changes { get; }[/code]
List of changes made.";

        // Example: Basic Usage
        _documentation["example_basic"] = @"[font_size=18][b]Example: Basic Usage[/b][/font_size]

[color=#aaaaaa]How to access GDShrapt API from your plugin.[/color]

[code]using GDShrapt.Plugin.Api;
using Godot;

[Tool]
public partial class MyCustomPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        // Wait for GDShrapt to initialize
        if (GDShraptApi.IsInitialized)
        {
            OnGDShraptReady();
        }
        else
        {
            GDShraptApi.Initialized += OnGDShraptReady;
        }
    }

    private void OnGDShraptReady()
    {
        var services = GDShraptApi.Services!;

        // Get all scripts in project
        var scripts = services.ProjectAnalyzer.Scripts;
        GD.Print($""Found {scripts.Count} scripts"");

        // Listen for changes
        services.ProjectAnalyzer.ScriptChanged += script =>
        {
            GD.Print($""Script changed: {script.ResourcePath}"");
        };
    }

    public override void _ExitTree()
    {
        GDShraptApi.Initialized -= OnGDShraptReady;
    }
}[/code]";

        // Example: Find References
        _documentation["example_find_refs"] = @"[font_size=18][b]Example: Find References[/b][/font_size]

[color=#aaaaaa]How to find all references to a symbol.[/color]

[code]var services = GDShraptApi.Services!;
var finder = services.ReferenceFinder;

// Find references by name
var refs = await finder.FindReferencesAsync(""player_speed"");

foreach (var reference in refs)
{
    GD.Print($""{reference.FilePath}:{reference.Line + 1}"");
    GD.Print($""  {reference.Kind}: {reference.ContextLine}"");
}

// Quick count without details
var count = finder.CountReferences(""player_speed"");
GD.Print($""Total references: {count}"");

// Get counts for all symbols in a file
var counts = finder.GetReferenceCountsForScript(
    ""res://scripts/player.gd"");

foreach (var (name, refCount) in counts)
{
    GD.Print($""{name}: {refCount} references"");
}[/code]";

        // Example: Rename
        _documentation["example_rename"] = @"[font_size=18][b]Example: Rename Symbol[/b][/font_size]

[color=#aaaaaa]How to rename a symbol across the project.[/color]

[code]var services = GDShraptApi.Services!;
var modifier = services.CodeModifier;

// Preview changes before renaming
var preview = await modifier.PreviewRenameAsync(
    ""res://scripts/player.gd"",
    line: 10,
    column: 4,
    ""new_variable_name"");

foreach (var change in preview)
{
    GD.Print($""{change.FilePath}:{change.StartLine + 1}"");
    GD.Print($""  '{change.OldText}' -> '{change.NewText}'"");
}

// Perform the rename
var result = await modifier.RenameAsync(
    ""res://scripts/player.gd"",
    line: 10,
    column: 4,
    ""new_variable_name"",
    new RenameOptions
    {
        RenameOnlyStrongTyped = false
    });

if (result.Success)
{
    GD.Print($""Renamed {result.ReferencesRenamed} references "");
    GD.Print($""in {result.FilesModified} files"");
}
else
{
    GD.PrintErr($""Rename failed: {result.ErrorMessage}"");
}[/code]";
    }
}
