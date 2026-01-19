namespace GDShrapt.Plugin;

/// <summary>
/// Canvas control for displaying the type flow graph.
/// Supports pan (drag), zoom (scroll wheel), and interactive node selection.
/// Wrapped in ScrollContainer for scrollbar support.
/// </summary>
internal partial class GDTypeFlowCanvas : ScrollContainer
{
    // Inner control for drawing
    private CanvasDrawingArea _drawingArea;

    // Pan/Zoom state
    private Vector2 _panOffset = Vector2.Zero;
    private float _zoom = 1.0f;
    private bool _isPanning;
    private Vector2 _lastMousePos;

    // Zoom limits
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 2.0f;
    private const float ZoomStep = 0.1f;

    // Content
    private List<GDTypeFlowBlock> _blocks = new();
    private List<GDTypeFlowNode> _nodes = new();
    private GDTypeFlowNode _focusNode;
    private GDTypeFlowLayoutEngine _layoutEngine;

    // Hover state
    private GDTypeFlowEdge _hoveredEdge;

    // Events
    public event Action<GDTypeFlowNode> BlockBodyClicked;
    public event Action<GDTypeFlowNode> BlockLabelClicked;
    public event Action<GDTypeFlowEdge> EdgeClicked;
    public event Action<GDTypeFlowEdge, Vector2> EdgeHoverStart;
    public event Action EdgeHoverEnd;

    // Edge drawing constants
    private const float EdgeHitRadius = 8f;
    private const float ArrowSize = 10f;

    // Background color (same as signature panel)
    private static readonly Color BackgroundColor = new(0.12f, 0.12f, 0.14f);
    private static readonly Color GridColor = new(0.16f, 0.16f, 0.19f);

    public GDTypeFlowCanvas()
    {
        _layoutEngine = new GDTypeFlowLayoutEngine();

        // ScrollContainer settings
        HorizontalScrollMode = ScrollMode.Auto;
        VerticalScrollMode = ScrollMode.Auto;

        // Create inner drawing area
        _drawingArea = new CanvasDrawingArea(this);
        _drawingArea.CustomMinimumSize = new Vector2(800, 600);
        AddChild(_drawingArea);
    }

    /// <summary>
    /// Displays the graph starting from the given root node.
    /// </summary>
    public void DisplayGraph(GDTypeFlowNode rootNode)
    {
        // Clear existing blocks
        foreach (var block in _blocks)
        {
            block.Clicked -= OnBlockClicked;
            block.DoubleClicked -= OnBlockDoubleClicked;
            block.LabelClicked -= OnBlockLabelClicked;
            block.QueueFree();
        }
        _blocks.Clear();
        _nodes.Clear();
        _focusNode = rootNode;

        if (rootNode == null)
        {
            _drawingArea.CustomMinimumSize = new Vector2(400, 300);
            _drawingArea.QueueRedraw();
            return;
        }

        // Compute layout
        _nodes = _layoutEngine.ComputeLayout(rootNode);

        // Create blocks for each node
        foreach (var node in _nodes)
        {
            var block = new GDTypeFlowBlock();
            block.SetNode(node);
            block.IsFocused = node == rootNode;
            block.IsSource = node.Level < 0;
            block.IsTarget = node.Level > 0;
            block.SetShowDetails(node == rootNode);
            block.UpdateVisualState();

            block.Clicked += OnBlockClicked;
            block.DoubleClicked += OnBlockDoubleClicked;
            block.LabelClicked += OnBlockLabelClicked;

            _drawingArea.AddChild(block);
            _blocks.Add(block);
        }

        // Calculate content size
        var bounds = _layoutEngine.GetBounds(_nodes);
        var padding = 60f;
        var contentSize = new Vector2(
            bounds.Size.X + bounds.Position.X + padding * 2,
            bounds.Size.Y + bounds.Position.Y + padding * 2
        );
        _drawingArea.CustomMinimumSize = contentSize;

        // Update block positions
        UpdateBlockPositions();
        _drawingArea.QueueRedraw();
    }

    /// <summary>
    /// Centers the canvas view on the specified node.
    /// </summary>
    public void CenterOnNode(GDTypeFlowNode node)
    {
        if (node == null)
            return;

        // Scroll to center the node
        var nodeCenter = node.Position + node.Size / 2;
        var viewportSize = Size;
        var scrollPos = nodeCenter - viewportSize / 2;

        ScrollHorizontal = (int)Math.Max(0, scrollPos.X);
        ScrollVertical = (int)Math.Max(0, scrollPos.Y);
    }

    /// <summary>
    /// Fits all nodes into the visible area.
    /// </summary>
    public void FitAllNodes()
    {
        if (_nodes.Count == 0)
            return;

        // Reset scroll to top-left
        ScrollHorizontal = 0;
        ScrollVertical = 0;
    }

    /// <summary>
    /// Updates block positions based on layout.
    /// </summary>
    private void UpdateBlockPositions()
    {
        var padding = 30f;

        for (int i = 0; i < _blocks.Count && i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            var block = _blocks[i];

            block.Position = node.Position + new Vector2(padding, padding);
            block.Size = node.Size;
        }
    }

    /// <summary>
    /// Scrolls the canvas to show the specified node.
    /// </summary>
    public void ScrollToNode(GDTypeFlowNode node)
    {
        if (node == null)
            return;

        CenterOnNode(node);
    }

    // Event handlers for block clicks
    private void OnBlockClicked(GDTypeFlowBlock block)
    {
        if (block?.Node != null)
        {
            BlockBodyClicked?.Invoke(block.Node);
        }
    }

    private void OnBlockDoubleClicked(GDTypeFlowBlock block)
    {
        if (block?.Node != null)
        {
            BlockLabelClicked?.Invoke(block.Node);
        }
    }

    private void OnBlockLabelClicked(GDTypeFlowBlock block)
    {
        if (block?.Node != null)
        {
            BlockLabelClicked?.Invoke(block.Node);
        }
    }

    /// <summary>
    /// Gets the current zoom level.
    /// </summary>
    public float Zoom => _zoom;

    /// <summary>
    /// Sets the zoom level.
    /// </summary>
    public void SetZoom(float zoom)
    {
        _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        UpdateBlockPositions();
        _drawingArea.QueueRedraw();
    }

    /// <summary>
    /// Inner control for drawing edges and background.
    /// </summary>
    private partial class CanvasDrawingArea : Control
    {
        private readonly GDTypeFlowCanvas _canvas;

        public CanvasDrawingArea(GDTypeFlowCanvas canvas)
        {
            _canvas = canvas;
            MouseFilter = MouseFilterEnum.Pass;
        }

        public override void _Draw()
        {
            // Draw background (same color as signature panel)
            DrawRect(new Rect2(Vector2.Zero, Size), BackgroundColor);

            // Draw subtle grid
            DrawGrid();

            // Draw edges with labels
            DrawEdges();
        }

        private void DrawGrid()
        {
            var gridSize = 50f;

            for (var x = 0f; x < Size.X; x += gridSize)
            {
                DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), GridColor, 1);
            }

            for (var y = 0f; y < Size.Y; y += gridSize)
            {
                DrawLine(new Vector2(0, y), new Vector2(Size.X, y), GridColor, 1);
            }
        }

        private void DrawEdges()
        {
            var padding = 30f;

            foreach (var node in _canvas._nodes)
            {
                foreach (var edge in node.OutgoingEdges)
                {
                    DrawEdge(edge, padding);
                }
            }
        }

        private void DrawEdge(GDTypeFlowEdge edge, float padding)
        {
            if (edge?.Source == null || edge.Target == null)
                return;

            var sourcePos = edge.Source.Position + new Vector2(padding, padding);
            var targetPos = edge.Target.Position + new Vector2(padding, padding);
            var sourceSize = edge.Source.Size;
            var targetSize = edge.Target.Size;

            // Determine connection points
            Vector2 start, end;

            if (edge.Source.Level < edge.Target.Level)
            {
                // Downward edge (inflow to focus, or focus to outflow)
                start = sourcePos + new Vector2(sourceSize.X / 2, sourceSize.Y);
                end = targetPos + new Vector2(targetSize.X / 2, 0);
            }
            else
            {
                // Upward edge
                start = sourcePos + new Vector2(sourceSize.X / 2, 0);
                end = targetPos + new Vector2(targetSize.X / 2, targetSize.Y);
            }

            // Get edge color
            var color = edge.GetEdgeColor();
            var lineWidth = edge.GetLineWidth();

            // Highlight hovered edge
            if (edge == _canvas._hoveredEdge)
            {
                color = color.Lightened(0.3f);
                lineWidth += 1;
            }

            // Draw bezier curve
            DrawBezierCurve(start, end, color, lineWidth, edge.IsDashed);

            // Draw arrow at target
            DrawArrow(end, start, end, color);

            // Draw edge label
            DrawEdgeLabel(edge, start, end, color);
        }

        private void DrawBezierCurve(Vector2 start, Vector2 end, Color color, float width, bool dashed)
        {
            var distance = start.DistanceTo(end);
            var controlOffset = Math.Min(distance * 0.5f, 60f);

            var control1 = start + new Vector2(0, controlOffset);
            var control2 = end - new Vector2(0, controlOffset);

            var segments = (int)Math.Max(10, distance / 10);
            var prevPoint = start;

            for (int i = 1; i <= segments; i++)
            {
                var t = (float)i / segments;
                var point = CubicBezier(start, control1, control2, end, t);

                if (dashed)
                {
                    if (i % 2 == 0)
                    {
                        DrawLine(prevPoint, point, color, width);
                    }
                }
                else
                {
                    DrawLine(prevPoint, point, color, width);
                }

                prevPoint = point;
            }
        }

        private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            var u = 1 - t;
            var tt = t * t;
            var uu = u * u;
            var uuu = uu * u;
            var ttt = tt * t;

            return uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
        }

        private void DrawArrow(Vector2 point, Vector2 from, Vector2 to, Color color)
        {
            var direction = (to - from).Normalized();
            var perpendicular = new Vector2(-direction.Y, direction.X);

            var arrowSize = ArrowSize;
            var arrowPoint1 = point - direction * arrowSize + perpendicular * arrowSize * 0.5f;
            var arrowPoint2 = point - direction * arrowSize - perpendicular * arrowSize * 0.5f;

            var points = new Vector2[] { point, arrowPoint1, arrowPoint2 };
            var colors = new Color[] { color, color, color };

            DrawPolygon(points, colors);
        }

        private void DrawEdgeLabel(GDTypeFlowEdge edge, Vector2 start, Vector2 end, Color color)
        {
            // Get label text based on edge kind and context
            var label = GetEdgeLabelText(edge);
            if (string.IsNullOrEmpty(label))
                return;

            // Position label at midpoint of edge
            var midPoint = (start + end) / 2;
            var labelOffset = new Vector2(8, -4);

            // Draw label background
            var font = ThemeDB.FallbackFont;
            var fontSize = 10;
            var labelSize = font.GetStringSize(label, HorizontalAlignment.Left, -1, fontSize);
            var bgRect = new Rect2(midPoint + labelOffset - new Vector2(2, labelSize.Y), labelSize + new Vector2(4, 4));

            DrawRect(bgRect, new Color(0.1f, 0.1f, 0.12f, 0.9f));

            // Draw label text
            DrawString(font, midPoint + labelOffset, label, HorizontalAlignment.Left, -1, fontSize, color.Lightened(0.2f));
        }

        private string GetEdgeLabelText(GDTypeFlowEdge edge)
        {
            if (edge == null)
                return null;

            var source = edge.Source;

            // Generate label based on source node kind with SourceType context
            return edge.Kind switch
            {
                GDTypeFlowEdgeKind.TypeFlow => source?.Kind switch
                {
                    GDTypeFlowNodeKind.TypeAnnotation => ": type",
                    GDTypeFlowNodeKind.Literal => "= literal",
                    GDTypeFlowNodeKind.MethodCall => GetMethodCallEdgeLabel(source),
                    GDTypeFlowNodeKind.IndexerAccess => GetIndexerEdgeLabel(source),
                    GDTypeFlowNodeKind.PropertyAccess => GetPropertyAccessEdgeLabel(source),
                    GDTypeFlowNodeKind.TypeCheck => "is check",
                    GDTypeFlowNodeKind.NullCheck => "null check",
                    GDTypeFlowNodeKind.Comparison => "compare",
                    GDTypeFlowNodeKind.MemberVariable => $".{source?.Label?.TrimStart('.')}",
                    GDTypeFlowNodeKind.ReturnValue => "returns",
                    _ => null
                },
                GDTypeFlowEdgeKind.Assignment => "=",
                GDTypeFlowEdgeKind.UnionMember => "|",
                GDTypeFlowEdgeKind.DuckConstraint => "duck",
                GDTypeFlowEdgeKind.Return => "→",
                _ => null
            };
        }

        /// <summary>
        /// Gets edge label for method call with source type context.
        /// </summary>
        private string GetMethodCallEdgeLabel(GDTypeFlowNode source)
        {
            if (source == null)
                return "call";

            // If we have SourceType, show it: "Dictionary.get()"
            if (!string.IsNullOrEmpty(source.SourceType) && source.SourceType != "Variant")
            {
                return $"{source.SourceType}.()";
            }

            // If we have SourceObjectName, show it: "result.()"
            if (!string.IsNullOrEmpty(source.SourceObjectName))
            {
                return $"{source.SourceObjectName}.()";
            }

            return "call";
        }

        /// <summary>
        /// Gets edge label for indexer access with source type context.
        /// </summary>
        private string GetIndexerEdgeLabel(GDTypeFlowNode source)
        {
            if (source == null)
                return "[i]";

            // If we have SourceType: "Dictionary[key]" or "Array[i]"
            if (!string.IsNullOrEmpty(source.SourceType) && source.SourceType != "Variant")
            {
                return $"{source.SourceType}[]";
            }

            // If we have SourceObjectName: "result[]"
            if (!string.IsNullOrEmpty(source.SourceObjectName))
            {
                return $"{source.SourceObjectName}[]";
            }

            return "[i]";
        }

        /// <summary>
        /// Gets edge label for property access with source type context.
        /// </summary>
        private string GetPropertyAccessEdgeLabel(GDTypeFlowNode source)
        {
            if (source == null)
                return ".prop";

            // If we have SourceType: "Vector2.x"
            if (!string.IsNullOrEmpty(source.SourceType) && source.SourceType != "Variant")
            {
                return $"{source.SourceType}.";
            }

            // If we have SourceObjectName: "obj."
            if (!string.IsNullOrEmpty(source.SourceObjectName))
            {
                return $"{source.SourceObjectName}.";
            }

            return ".prop";
        }
    }
}
