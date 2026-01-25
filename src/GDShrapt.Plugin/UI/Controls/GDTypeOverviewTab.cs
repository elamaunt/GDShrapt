using GDShrapt.CLI.Core;
using GDShrapt.Semantics;

namespace GDShrapt.Plugin;

/// <summary>
/// Overview tab for Type Breakdown panel.
/// Shows a compact list of parameters, return type, and local variables with their types and confidence.
/// </summary>
internal partial class GDTypeOverviewTab : ScrollContainer
{
    private VBoxContainer _content;

    // Colors
    private static readonly Color HeaderColor = new(0.6f, 0.6f, 0.6f);
    private static readonly Color HintColor = new(0.5f, 0.5f, 0.5f);
    private static readonly Color HighConfidenceColor = new(0.3f, 0.8f, 0.3f);
    private static readonly Color MediumConfidenceColor = new(1.0f, 0.7f, 0.3f);
    private static readonly Color LowConfidenceColor = new(0.8f, 0.3f, 0.3f);

    /// <summary>
    /// Fired when a node is clicked (single click).
    /// </summary>
    public event Action<GDTypeFlowNode> NodeClicked;

    /// <summary>
    /// Fired when a node is double-clicked.
    /// </summary>
    public event Action<GDTypeFlowNode> NodeDoubleClicked;

    public GDTypeOverviewTab()
    {
        HorizontalScrollMode = ScrollMode.Disabled;
        VerticalScrollMode = ScrollMode.Auto;
        // ExpandFill to take all available space in TabContainer
        SizeFlagsVertical = SizeFlags.ExpandFill;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 4);
        _content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        // Content inside ScrollContainer should be ShrinkBegin (scrollable content)
        _content.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        AddChild(_content);
    }

    /// <summary>
    /// Displays overview information for the given node.
    /// </summary>
    public void DisplayOverview(GDTypeFlowNode node, GDSemanticModel semanticModel = null)
    {
        Clear();

        if (node == null)
            return;

        // Parameters section (if applicable)
        var parameters = GetParameters(node, semanticModel);
        if (parameters.Count > 0)
        {
            AddSection("Parameters", parameters);
        }

        // Return type section (if applicable)
        var returnInfo = GetReturnTypeInfo(node, semanticModel);
        if (returnInfo != null)
        {
            AddSection("Return Type", new List<TypeInfo> { returnInfo });
        }

        // Local variables section
        var locals = GetLocalVariables(node, semanticModel);
        if (locals.Count > 0)
        {
            AddSection("Local Variables", locals);
        }

        // Inflows summary
        if (node.Inflows.Count > 0)
        {
            AddInflowsSummary(node);
        }

        // Outflows summary
        if (node.Outflows.Count > 0)
        {
            AddOutflowsSummary(node);
        }
    }

    /// <summary>
    /// Clears the overview content.
    /// </summary>
    public void Clear()
    {
        foreach (var child in _content.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void AddSection(string title, List<TypeInfo> items)
    {
        // Section header
        var header = new Label { Text = title };
        header.AddThemeFontSizeOverride("font_size", 12);
        header.AddThemeColorOverride("font_color", HeaderColor);
        _content.AddChild(header);

        // Section items
        foreach (var item in items)
        {
            var row = CreateTypeInfoRow(item);
            _content.AddChild(row);
        }

        // Add spacing after section
        var spacer = new Control { CustomMinimumSize = new Vector2(0, 8) };
        _content.AddChild(spacer);
    }

    private Control CreateTypeInfoRow(TypeInfo info)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);

        // Bullet point
        var bullet = new Label { Text = "  •" };
        bullet.AddThemeColorOverride("font_color", HintColor);
        row.AddChild(bullet);

        // Name
        var nameButton = new Button
        {
            Text = $"{info.Name}:",
            Flat = true,
            TooltipText = $"Click to navigate to {info.Name}"
        };
        nameButton.AddThemeFontSizeOverride("font_size", 11);
        nameButton.Pressed += () => OnNodeClicked(info);
        row.AddChild(nameButton);

        // Type with confidence color
        var typeLabel = new Label { Text = info.Type };
        typeLabel.AddThemeFontSizeOverride("font_size", 11);
        typeLabel.AddThemeColorOverride("font_color", GetConfidenceColor(info.Confidence));
        row.AddChild(typeLabel);

        // Confidence badge
        var badge = new GDConfidenceBadge();
        badge.SetConfidence(info.Confidence);
        badge.SetCompactMode(true);
        row.AddChild(badge);

        // Source hint
        if (!string.IsNullOrEmpty(info.SourceHint))
        {
            var sourceLabel = new Label { Text = $"← {info.SourceHint}" };
            sourceLabel.AddThemeFontSizeOverride("font_size", 10);
            sourceLabel.AddThemeColorOverride("font_color", HintColor);
            row.AddChild(sourceLabel);
        }

        return row;
    }

    private void AddInflowsSummary(GDTypeFlowNode node)
    {
        var header = new Label { Text = $"Inflows ({node.Inflows.Count})" };
        header.AddThemeFontSizeOverride("font_size", 12);
        header.AddThemeColorOverride("font_color", HeaderColor);
        _content.AddChild(header);

        // Show first few inflows
        foreach (var inflow in node.Inflows.Take(3))
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 4);

            var bullet = new Label { Text = "  ↓" };
            bullet.AddThemeColorOverride("font_color", new Color(0.4f, 0.7f, 0.4f));
            row.AddChild(bullet);

            var inflowButton = new Button
            {
                Text = $"{inflow.Label}: {inflow.Type ?? "Variant"}",
                Flat = true,
                TooltipText = $"Click to view {inflow.Label}"
            };
            inflowButton.AddThemeFontSizeOverride("font_size", 11);
            inflowButton.Pressed += () => NodeDoubleClicked?.Invoke(inflow);
            row.AddChild(inflowButton);

            _content.AddChild(row);
        }

        if (node.Inflows.Count > 3)
        {
            var moreLabel = new Label { Text = $"  ... and {node.Inflows.Count - 3} more" };
            moreLabel.AddThemeFontSizeOverride("font_size", 10);
            moreLabel.AddThemeColorOverride("font_color", HintColor);
            _content.AddChild(moreLabel);
        }

        var spacer = new Control { CustomMinimumSize = new Vector2(0, 8) };
        _content.AddChild(spacer);
    }

    private void AddOutflowsSummary(GDTypeFlowNode node)
    {
        var header = new Label { Text = $"Outflows ({node.Outflows.Count})" };
        header.AddThemeFontSizeOverride("font_size", 12);
        header.AddThemeColorOverride("font_color", HeaderColor);
        _content.AddChild(header);

        // Show first few outflows
        foreach (var outflow in node.Outflows.Take(3))
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 4);

            var bullet = new Label { Text = "  ↑" };
            bullet.AddThemeColorOverride("font_color", new Color(0.7f, 0.4f, 0.4f));
            row.AddChild(bullet);

            var outflowButton = new Button
            {
                Text = $"{outflow.Label}: {outflow.Type ?? "Variant"}",
                Flat = true,
                TooltipText = $"Click to view {outflow.Label}"
            };
            outflowButton.AddThemeFontSizeOverride("font_size", 11);
            outflowButton.Pressed += () => NodeDoubleClicked?.Invoke(outflow);
            row.AddChild(outflowButton);

            _content.AddChild(row);
        }

        if (node.Outflows.Count > 3)
        {
            var moreLabel = new Label { Text = $"  ... and {node.Outflows.Count - 3} more" };
            moreLabel.AddThemeFontSizeOverride("font_size", 10);
            moreLabel.AddThemeColorOverride("font_color", HintColor);
            _content.AddChild(moreLabel);
        }
    }

    private void OnNodeClicked(TypeInfo info)
    {
        if (info.Node != null)
        {
            NodeClicked?.Invoke(info.Node);
        }
    }

    private List<TypeInfo> GetParameters(GDTypeFlowNode node, GDSemanticModel semanticModel)
    {
        var result = new List<TypeInfo>();

        // Get parameters from inflows that are of type Parameter
        foreach (var inflow in node.Inflows)
        {
            if (inflow.Kind == GDTypeFlowNodeKind.Parameter)
            {
                result.Add(new TypeInfo
                {
                    Name = inflow.Label,
                    Type = inflow.Type ?? "Variant",
                    Confidence = inflow.Confidence,
                    SourceHint = GetSourceHint(inflow),
                    Node = inflow
                });
            }
        }

        return result;
    }

    private TypeInfo GetReturnTypeInfo(GDTypeFlowNode node, GDSemanticModel semanticModel)
    {
        // Find return type from outflows
        foreach (var outflow in node.Outflows)
        {
            if (outflow.Kind == GDTypeFlowNodeKind.ReturnValue)
            {
                return new TypeInfo
                {
                    Name = "return",
                    Type = outflow.Type ?? "void",
                    Confidence = outflow.Confidence,
                    SourceHint = GetSourceHint(outflow),
                    Node = outflow
                };
            }
        }

        // Check if node itself is a return type
        if (node.Kind == GDTypeFlowNodeKind.ReturnValue)
        {
            return new TypeInfo
            {
                Name = "return",
                Type = node.Type ?? "void",
                Confidence = node.Confidence,
                SourceHint = GetSourceHint(node),
                Node = node
            };
        }

        return null;
    }

    private List<TypeInfo> GetLocalVariables(GDTypeFlowNode node, GDSemanticModel semanticModel)
    {
        var result = new List<TypeInfo>();

        // Get local variables from inflows
        foreach (var inflow in node.Inflows)
        {
            if (inflow.Kind == GDTypeFlowNodeKind.LocalVariable)
            {
                result.Add(new TypeInfo
                {
                    Name = inflow.Label,
                    Type = inflow.Type ?? "Variant",
                    Confidence = inflow.Confidence,
                    SourceHint = GetSourceHint(inflow),
                    Node = inflow
                });
            }
        }

        // Also check if node itself is a local variable
        if (node.Kind == GDTypeFlowNodeKind.LocalVariable)
        {
            // Show inflows as sources
            foreach (var inflow in node.Inflows)
            {
                if (inflow.Kind != GDTypeFlowNodeKind.LocalVariable)
                {
                    result.Add(new TypeInfo
                    {
                        Name = inflow.Label,
                        Type = inflow.Type ?? "Variant",
                        Confidence = inflow.Confidence,
                        SourceHint = GetSourceHint(inflow),
                        Node = inflow
                    });
                }
            }
        }

        return result;
    }

    private string GetSourceHint(GDTypeFlowNode node)
    {
        return node.Kind switch
        {
            GDTypeFlowNodeKind.TypeAnnotation => "Type annotation",
            GDTypeFlowNodeKind.Literal => "Literal value",
            GDTypeFlowNodeKind.MethodCall => $"Method call",
            GDTypeFlowNodeKind.PropertyAccess => "Property access",
            GDTypeFlowNodeKind.IndexerAccess => "Indexer access",
            GDTypeFlowNodeKind.TypeCheck => "Type check",
            GDTypeFlowNodeKind.NullCheck => "Null check",
            GDTypeFlowNodeKind.Assignment => "Assignment",
            GDTypeFlowNodeKind.Parameter => "Parameter",
            GDTypeFlowNodeKind.MemberVariable => "Member variable",
            GDTypeFlowNodeKind.LocalVariable => "Local variable",
            GDTypeFlowNodeKind.ReturnValue => "Return statement",
            _ => node.Description ?? ""
        };
    }

    private static Color GetConfidenceColor(float confidence)
    {
        return confidence switch
        {
            >= 0.8f => HighConfidenceColor,
            >= 0.5f => MediumConfidenceColor,
            _ => LowConfidenceColor
        };
    }

    /// <summary>
    /// Internal helper class for type information.
    /// </summary>
    private class TypeInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public float Confidence { get; set; }
        public string SourceHint { get; set; }
        public GDTypeFlowNode Node { get; set; }
    }
}
