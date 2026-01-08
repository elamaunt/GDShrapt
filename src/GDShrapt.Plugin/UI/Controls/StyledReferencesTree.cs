using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GDShrapt.Plugin.UI;

/// <summary>
/// Custom Tree control with syntax highlighting support for reference display.
/// Highlights the matched symbol within each reference context line.
/// Uses custom draw callbacks for precise rendering like Godot editor.
/// </summary>
internal partial class StyledReferencesTree : Tree
{
    private HeaderCustomDrawObject _headerCustomDrawHolder;
    private CellCustomDrawObject _cellCustomDrawHolder;
    private Callable _headerDrawCallable;
    private Callable _cellDrawCallable;
    private Font _font;
    private int _fontSize;

    private TreeItem _mainRootItem;
    private readonly Dictionary<string, TreeItem> _rootItems = new();
    private readonly Dictionary<TreeItem, ReferenceItem> _itemToReference = new();
    private readonly bool _checkBoxesEnabled;

    private bool _scrolledToFirstChecked;
    private bool _initialized;

    private Color _highlightColor = new Color(1.0f, 0.9f, 0.0f, 0.3f); // Yellow highlight

    public Action<Variant, Variant> ItemSelectedHandler { get; set; }
    public IReadOnlyDictionary<string, TreeItem> RootItems => new ReadOnlyDictionary<string, TreeItem>(_rootItems);

    public StyledReferencesTree(bool checkBoxesEnabled = false)
    {
        _checkBoxesEnabled = checkBoxesEnabled;

        AddThemeFontSizeOverride("font_size", 14);
        AddThemeConstantOverride("inner_item_margin_left", 0);
        AddThemeConstantOverride("inner_item_margin_right", 0);

        AllowRmbSelect = true;

        SizeFlagsVertical = SizeFlags.ExpandFill;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;
        Columns = 2;

        SelectMode = SelectModeEnum.Row;
        HideRoot = true;

        if (checkBoxesEnabled)
            SetColumnCustomMinimumWidth(0, 64);
        else
            SetColumnCustomMinimumWidth(0, 64);

        SetColumnClipContent(0, true);
        SetColumnClipContent(1, false);

        SetColumnExpand(0, false);
        SetColumnExpand(1, true);
        SetColumnExpandRatio(0, 0);
        SetColumnExpandRatio(1, 1);

        ItemEdited += OnItemEdited;
        ItemSelected += OnItemSelected;

        Resized += OnResized;
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;
        Logger.Debug($"[StyledReferencesTree] EnsureInitialized starting...");

        // Get editor font for code display - must be done after node is in tree
        Font editorFont = null;
        try
        {
            editorFont = GetThemeFont("source", "EditorFonts");
            Logger.Debug($"[StyledReferencesTree] Got EditorFonts/source: {editorFont != null}");
        }
        catch (Exception ex)
        {
            Logger.Debug($"[StyledReferencesTree] EditorFonts/source failed: {ex.Message}");
        }

        // Try multiple font sources
        if (editorFont == null)
        {
            try
            {
                editorFont = GetThemeFont("font", "CodeEdit");
                Logger.Debug($"[StyledReferencesTree] Got CodeEdit/font: {editorFont != null}");
            }
            catch (Exception ex)
            {
                Logger.Debug($"[StyledReferencesTree] CodeEdit/font failed: {ex.Message}");
            }
        }

        if (editorFont == null)
        {
            try
            {
                editorFont = GetThemeFont("font");
                Logger.Debug($"[StyledReferencesTree] Got default font: {editorFont != null}");
            }
            catch (Exception ex)
            {
                Logger.Debug($"[StyledReferencesTree] default font failed: {ex.Message}");
            }
        }

        // Final fallback
        editorFont ??= ThemeDB.FallbackFont;
        Logger.Debug($"[StyledReferencesTree] Final editorFont: {editorFont != null}, type: {editorFont?.GetType().Name}");

        if (editorFont != null)
            AddThemeFontOverride("font", editorFont);

        _font = editorFont ?? ThemeDB.FallbackFont;
        _fontSize = GetThemeFontSize("font_size");

        // Ensure font size is valid
        if (_fontSize <= 0)
            _fontSize = 14;

        Logger.Debug($"[StyledReferencesTree] _font={_font != null}, _fontSize={_fontSize}");

        // Create custom draw holders - stored as fields to prevent GC
        _headerCustomDrawHolder = new HeaderCustomDrawObject(this);
        _cellCustomDrawHolder = new CellCustomDrawObject(this);

        // Create callables once and store them
        _headerDrawCallable = Callable.From<TreeItem, Rect2>(_headerCustomDrawHolder.CustomDraw);
        _cellDrawCallable = Callable.From<TreeItem, Rect2>(_cellCustomDrawHolder.CustomDraw);

        Logger.Debug($"[StyledReferencesTree] EnsureInitialized complete, holders and callables created");
    }

    private void OnResized()
    {
        SetColumnCustomMinimumWidth(1, (int)(Size.X - 15f));
    }

    private void OnItemEdited()
    {
        var item = GetEdited();
        var column = GetEditedColumn();

        if (item == null)
            return;

        if (column == 0)
            UpdateCheckedState(item);
    }

    private void UpdateCheckedState(TreeItem item)
    {
        if (item.IsChecked(0))
        {
            var parent = item.GetParent();

            while (parent != null)
            {
                parent.SetChecked(0, parent.GetChildren().All(x => x.IsChecked(0)));
                parent = parent.GetParent();
            }

            void Update(TreeItem treeItem)
            {
                var children = treeItem.GetChildren();
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    child.SetChecked(0, true);
                    Update(child);
                }
            }

            Update(item);
        }
        else
        {
            var parent = item.GetParent();

            while (parent != null)
            {
                parent.SetChecked(0, false);
                parent = parent.GetParent();
            }

            void Update(TreeItem treeItem)
            {
                var children = treeItem.GetChildren();
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    child.SetChecked(0, false);
                    Update(child);
                }
            }

            Update(item);
        }
    }

    private void OnItemSelected()
    {
        var selectedItem = GetSelected();

        if (selectedItem == null)
            return;

        var data = selectedItem.GetMetadata(0).As<MetadataContainer>();

        if (data == null)
            return;

        if (!data.Clickable)
            return;

        ItemSelectedHandler?.Invoke(data.GetMetadata(0), data.GetMetadata(1));
    }

    /// <summary>
    /// Clears tree and all metadata.
    /// </summary>
    public new void Clear()
    {
        base.Clear();
        _scrolledToFirstChecked = false;
        _rootItems.Clear();
        _itemToReference.Clear();
        _mainRootItem = null;
    }

    /// <summary>
    /// Creates a file header item with reference count.
    /// </summary>
    public TreeItem CreateFileItem(TreeItem parent, string fileName, int count, string fullPath)
    {
        EnsureInitialized();
        var stringSize = _font.GetStringSize(fileName, fontSize: _fontSize, alignment: HorizontalAlignment.Left, width: -1);

        if (!_rootItems.TryGetValue(fullPath, out var rootItem))
        {
            if (_mainRootItem == null)
            {
                _mainRootItem = CreateItem(parent);

                if (_checkBoxesEnabled)
                {
                    _mainRootItem.SetCellMode(0, TreeItem.TreeCellMode.Check);
                    _mainRootItem.SetChecked(0, false);
                    _mainRootItem.SetEditable(0, true);
                }

                _mainRootItem.SetMetadata(0, new MetadataContainer(new HeaderLayoutData()
                {
                    LeftOffset = _checkBoxesEnabled ? 24 : 0,
                    Color = _highlightColor,
                    Text = "*",
                    Font = _font,
                    FontSize = _fontSize,
                    LineHeight = stringSize.Y
                })
                {
                    Clickable = false
                });

                _mainRootItem.SetCellMode(1, TreeItem.TreeCellMode.Custom);
                _mainRootItem.SetCustomDrawCallback(1, _headerDrawCallable);
                Logger.Debug($"[StyledReferencesTree] Set header callback for _mainRootItem");
            }

            rootItem = CreateItem(_mainRootItem);
            _rootItems[fullPath] = rootItem;

            if (_checkBoxesEnabled)
            {
                rootItem.SetCellMode(0, TreeItem.TreeCellMode.Check);
                rootItem.SetChecked(0, false);
                rootItem.SetEditable(0, true);
            }

            rootItem.SetMetadata(0, new MetadataContainer(new HeaderLayoutData()
            {
                LeftOffset = _checkBoxesEnabled ? 24 : 0,
                Color = _highlightColor,
                Text = $"{fileName} ({count})",
                Font = _font,
                FontSize = _fontSize,
                Layer = 1,
                LineHeight = stringSize.Y
            }, fullPath)
            {
                Clickable = false
            });

            rootItem.SetCellMode(1, TreeItem.TreeCellMode.Custom);
            rootItem.SetCustomDrawCallback(1, _headerDrawCallable);
            Logger.Debug($"[StyledReferencesTree] Set header callback for rootItem '{fileName}'");
        }

        return rootItem;
    }

    /// <summary>
    /// Creates a reference item with highlight information.
    /// </summary>
    public TreeItem CreateReferenceItem(TreeItem parent, ReferenceItem reference)
    {
        EnsureInitialized();
        var contextLine = reference.ContextLine ?? "";
        var highlightStart = reference.HighlightStart;
        var highlightEnd = reference.HighlightEnd;

        // Calculate string sizes for positioning
        var beforeText = highlightStart > 0 && highlightStart <= contextLine.Length
            ? contextLine.Substring(0, highlightStart)
            : "";
        var highlightText = highlightStart >= 0 && highlightEnd > highlightStart && highlightEnd <= contextLine.Length
            ? contextLine.Substring(highlightStart, highlightEnd - highlightStart)
            : "";

        var stringSize = _font.GetStringSize(beforeText.TrimStart(), fontSize: _fontSize, alignment: HorizontalAlignment.Left, width: -1);
        var highlightSize = _font.GetStringSize(highlightText, fontSize: _fontSize, alignment: HorizontalAlignment.Left, width: -1);

        var item = CreateItem(parent);

        item.SetAutowrapMode(0, TextServer.AutowrapMode.Off);
        item.SetAutowrapMode(1, TextServer.AutowrapMode.Off);
        item.SetTextOverrunBehavior(1, TextServer.OverrunBehavior.TrimEllipsis);
        item.SetCustomFont(1, _font);
        item.SetCustomFontSize(1, _fontSize);
        item.SetCellMode(1, TreeItem.TreeCellMode.Custom);
        item.SetTextAlignment(0, HorizontalAlignment.Right);
        item.SetTextAlignment(1, HorizontalAlignment.Left);
        item.SetCustomDrawCallback(1, _cellDrawCallable);
        Logger.Debug($"[StyledReferencesTree] Set cell callback for reference line {reference.Line}");

        item.SetExpandRight(0, false);

        if (_checkBoxesEnabled)
        {
            item.SetCellMode(0, TreeItem.TreeCellMode.Check);
            item.SetChecked(0, true); // Selected by default
            item.SetEditable(0, true);

            if (!_scrolledToFirstChecked)
            {
                _scrolledToFirstChecked = true;
                ScrollToItem(item);
            }
        }

        float scale;
        try
        {
            scale = EditorInterface.Singleton.GetEditorScale();
        }
        catch
        {
            scale = 1.0f;
        }

        var offset = stringSize.X - 1 * scale;
        offset += 1 * scale;

        // Store reference mapping
        _itemToReference[item] = reference;

        // Create cell layout data for custom drawing
        item.SetMetadata(0, new MetadataContainer(reference, new CellLayoutData()
        {
            LeftOffset = _checkBoxesEnabled ? 24 : 0,
            DrawOffset = offset,
            DrawWidth = highlightSize.X - 1 * scale,
            Color = _highlightColor,
            FullText = contextLine.TrimStart(),
            LinePrefix = $"{reference.Line + 1}: ",
            Font = _font,
            FontSize = _fontSize,
            LineHeight = highlightSize.Y > 0 ? highlightSize.Y : _fontSize
        }));

        // Update parent checked state
        var parentItem = item.GetParent();
        while (parentItem != null)
        {
            parentItem.SetChecked(0, parentItem.GetChildren().All(x => x.IsChecked(0)));
            parentItem = parentItem.GetParent();
        }

        return item;
    }

    /// <summary>
    /// Gets the ReferenceItem for a tree item.
    /// </summary>
    public ReferenceItem GetReferenceForItem(TreeItem item)
    {
        return _itemToReference.TryGetValue(item, out var reference) ? reference : null;
    }

    /// <summary>
    /// Sets the highlight color.
    /// </summary>
    public void SetHighlightColor(Color backgroundColor)
    {
        _highlightColor = backgroundColor;
        QueueRedraw();
    }

    /// <summary>
    /// Selects all checkboxes.
    /// </summary>
    public void SelectAll()
    {
        if (_mainRootItem != null)
        {
            _mainRootItem.SetChecked(0, true);
            UpdateCheckedState(_mainRootItem);
        }
    }

    /// <summary>
    /// Deselects all checkboxes.
    /// </summary>
    public new void DeselectAll()
    {
        if (_mainRootItem != null)
        {
            _mainRootItem.SetChecked(0, false);
            UpdateCheckedState(_mainRootItem);
        }
    }

    /// <summary>
    /// Layout data for cell custom drawing.
    /// </summary>
    internal partial class CellLayoutData : GodotObject
    {
        public Font Font { get; set; }
        public int FontSize { get; set; }
        public float DrawOffset { get; set; }
        public float DrawWidth { get; set; }
        public float LineHeight { get; set; }
        public Color Color { get; set; }
        public string FullText { get; set; }
        public string LinePrefix { get; set; }
        public int LeftOffset { get; set; }
    }

    /// <summary>
    /// Layout data for header custom drawing.
    /// </summary>
    internal partial class HeaderLayoutData : GodotObject
    {
        public int Layer { get; set; }
        public Font Font { get; set; }
        public int FontSize { get; set; }
        public float LineHeight { get; set; }
        public Color Color { get; set; }
        public string Text { get; set; }
        public int LeftOffset { get; set; }
    }
}

/// <summary>
/// Container for metadata stored in tree items.
/// </summary>
internal partial class MetadataContainer : GodotObject
{
    private readonly Variant[] _metadatas;

    public bool Clickable { get; set; } = true;

    public MetadataContainer(params Variant[] metadatas)
    {
        _metadatas = metadatas;
    }

    public Variant GetMetadata(int index)
    {
        if (_metadatas.Length <= index)
            return default;
        if (0 > index)
            return default;
        return _metadatas[index];
    }
}

/// <summary>
/// Custom draw handler for reference cells with syntax highlighting.
/// </summary>
internal partial class CellCustomDrawObject : GodotObject
{
    private readonly Tree _tree;
    private static int _logCounter = 0;

    public CellCustomDrawObject(Tree tree)
    {
        _tree = tree;
        Logger.Debug($"[CellCustomDrawObject] Created with tree: {tree != null}");
    }

    public void CustomDraw(TreeItem item, Rect2 rect)
    {
        _logCounter++;
        var shouldLog = _logCounter <= 10; // Log only first 10 calls to avoid spam

        if (shouldLog)
            Logger.Debug($"[CellCustomDraw] Called #{_logCounter}, rect: pos=({rect.Position.X:F1},{rect.Position.Y:F1}), size=({rect.Size.X:F1},{rect.Size.Y:F1})");

        var cellData = item.GetMetadata(0).As<MetadataContainer>();

        if (cellData == null)
        {
            if (shouldLog) Logger.Debug($"[CellCustomDraw] cellData is NULL");
            return;
        }

        var data = cellData.GetMetadata(1).As<StyledReferencesTree.CellLayoutData>();

        if (data == null)
        {
            if (shouldLog) Logger.Debug($"[CellCustomDraw] CellLayoutData is NULL (metadata[1] type: {cellData.GetMetadata(1).VariantType})");
            return;
        }

        if (shouldLog)
            Logger.Debug($"[CellCustomDraw] CellLayoutData: FullText='{data.FullText?.Substring(0, Math.Min(30, data.FullText?.Length ?? 0))}...', LinePrefix='{data.LinePrefix}', Font={data.Font != null}, FontSize={data.FontSize}");

        if (string.IsNullOrEmpty(data.FullText))
        {
            if (shouldLog) Logger.Debug($"[CellCustomDraw] FullText is empty");
            return;
        }

        // Use fallback font if needed
        var font = data.Font ?? ThemeDB.FallbackFont;
        if (font == null)
        {
            if (shouldLog) Logger.Debug($"[CellCustomDraw] Font is NULL even after fallback!");
            return;
        }

        var fontSize = data.FontSize > 0 ? data.FontSize : 14;
        var fixedOffset = 44 + data.LeftOffset;

        if (shouldLog)
            Logger.Debug($"[CellCustomDraw] Using font: {font.GetType().Name}, fontSize={fontSize}, fixedOffset={fixedOffset}");

        // Calculate Y position for text baseline (center vertically)
        var ascent = font.GetAscent(fontSize);
        var textY = rect.Position.Y + (rect.Size.Y + ascent) / 2;

        if (shouldLog)
            Logger.Debug($"[CellCustomDraw] ascent={ascent:F1}, textY={textY:F1}");

        // Draw line prefix (line number)
        var prefixWidth = font.GetStringSize(data.LinePrefix ?? "", fontSize: fontSize).X;

        if (shouldLog)
            Logger.Debug($"[CellCustomDraw] Drawing prefix at ({fixedOffset}, {textY:F1}), prefixWidth={prefixWidth:F1}");

        _tree.DrawString(font, new Vector2(fixedOffset, textY),
            data.LinePrefix ?? "", HorizontalAlignment.Left, fontSize: fontSize, modulate: new Color(0.5f, 0.5f, 0.6f));

        var textOffset = fixedOffset + prefixWidth;

        // Draw highlight background
        var x = data.DrawOffset + textOffset;
        var highlightRect = new Rect2(x, rect.Position.Y, data.DrawWidth, rect.Size.Y);

        if (highlightRect.Size.X > 0)
            _tree.DrawRect(highlightRect, data.Color);

        if (shouldLog)
            Logger.Debug($"[CellCustomDraw] Drawing text at ({textOffset}, {textY:F1}), text='{data.FullText?.Substring(0, Math.Min(20, data.FullText?.Length ?? 0))}...'");

        // Draw full text
        _tree.DrawString(font, new Vector2(textOffset, textY),
            data.FullText, HorizontalAlignment.Left, fontSize: fontSize, modulate: new Color(0.7f, 0.7f, 0.7f));

        if (shouldLog)
            Logger.Debug($"[CellCustomDraw] Draw complete for item");
    }
}

/// <summary>
/// Custom draw handler for header items (file names).
/// </summary>
internal partial class HeaderCustomDrawObject : GodotObject
{
    private readonly Tree _tree;
    private static int _logCounter = 0;

    public HeaderCustomDrawObject(Tree tree)
    {
        _tree = tree;
        Logger.Debug($"[HeaderCustomDrawObject] Created with tree: {tree != null}");
    }

    public void CustomDraw(TreeItem item, Rect2 rect)
    {
        _logCounter++;
        var shouldLog = _logCounter <= 10;

        if (shouldLog)
            Logger.Debug($"[HeaderCustomDraw] Called #{_logCounter}, rect: pos=({rect.Position.X:F1},{rect.Position.Y:F1}), size=({rect.Size.X:F1},{rect.Size.Y:F1})");

        var cellData = item.GetMetadata(0).As<MetadataContainer>();

        if (cellData == null)
        {
            if (shouldLog) Logger.Debug($"[HeaderCustomDraw] cellData is NULL");
            return;
        }

        var data = cellData.GetMetadata(0).As<StyledReferencesTree.HeaderLayoutData>();

        if (data == null)
        {
            if (shouldLog) Logger.Debug($"[HeaderCustomDraw] HeaderLayoutData is NULL");
            return;
        }

        if (shouldLog)
            Logger.Debug($"[HeaderCustomDraw] HeaderLayoutData: Text='{data.Text}', Font={data.Font != null}, FontSize={data.FontSize}, Layer={data.Layer}");

        if (string.IsNullOrEmpty(data.Text))
        {
            if (shouldLog) Logger.Debug($"[HeaderCustomDraw] Text is empty");
            return;
        }

        // Use fallback font if needed
        var font = data.Font ?? ThemeDB.FallbackFont;
        if (font == null)
        {
            if (shouldLog) Logger.Debug($"[HeaderCustomDraw] Font is NULL even after fallback!");
            return;
        }

        var fontSize = data.FontSize > 0 ? data.FontSize : 14;
        var fixedOffset = 18 + 14 * data.Layer + data.LeftOffset;

        if (shouldLog)
            Logger.Debug($"[HeaderCustomDraw] Using font: {font.GetType().Name}, fontSize={fontSize}, fixedOffset={fixedOffset}");

        // Calculate Y position for text baseline (center vertically)
        var ascent = font.GetAscent(fontSize);
        var textY = rect.Position.Y + (rect.Size.Y + ascent) / 2;

        if (shouldLog)
            Logger.Debug($"[HeaderCustomDraw] Drawing text at ({fixedOffset}, {textY:F1}), text='{data.Text}'");

        _tree.DrawString(font, new Vector2(fixedOffset, textY),
            data.Text, HorizontalAlignment.Left, fontSize: fontSize, modulate: new Color(0.8f, 0.82f, 0.92f));

        _tree.DrawRect(new Rect2(0, rect.Position.Y, rect.Size.X + 48, rect.Size.Y),
            new Color(0.95f, 0.95f, 1f, 0.01f * (2 - data.Layer)));

        if (shouldLog)
            Logger.Debug($"[HeaderCustomDraw] Draw complete");
    }
}
