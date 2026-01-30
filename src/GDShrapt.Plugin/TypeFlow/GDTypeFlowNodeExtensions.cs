using GDShrapt.CLI.Core;

namespace GDShrapt.Plugin;

/// <summary>
/// Extension methods for GDTypeFlowNode to provide Godot-specific UI functionality.
/// </summary>
internal static class GDTypeFlowNodeExtensions
{
    /// <summary>
    /// Gets the position as a Godot Vector2.
    /// </summary>
    public static Vector2 GetPosition(this GDTypeFlowNode node)
    {
        return new Vector2(node.PositionX, node.PositionY);
    }

    /// <summary>
    /// Sets the position from a Godot Vector2.
    /// </summary>
    public static void SetPosition(this GDTypeFlowNode node, Vector2 position)
    {
        node.PositionX = position.X;
        node.PositionY = position.Y;
    }

    /// <summary>
    /// Gets the size as a Godot Vector2.
    /// </summary>
    public static Vector2 GetSize(this GDTypeFlowNode node)
    {
        return new Vector2(node.Width, node.Height);
    }

    /// <summary>
    /// Sets the size from a Godot Vector2.
    /// </summary>
    public static void SetSize(this GDTypeFlowNode node, Vector2 size)
    {
        node.Width = size.X;
        node.Height = size.Y;
    }

    /// <summary>
    /// Gets the color associated with this node's confidence level.
    /// </summary>
    public static Color GetConfidenceColor(this GDTypeFlowNode node)
    {
        var level = node.GetConfidenceLevel();
        return level switch
        {
            GDTypeFlowConfidenceLevel.High => new Color(0.3f, 0.8f, 0.3f),   // Зелёный
            GDTypeFlowConfidenceLevel.Medium => new Color(1.0f, 0.7f, 0.3f), // Оранжевый
            GDTypeFlowConfidenceLevel.Low => new Color(0.8f, 0.3f, 0.3f),    // Красный
            _ => new Color(0.5f, 0.5f, 0.5f)
        };
    }

    /// <summary>
    /// Gets the icon name for this node kind.
    /// </summary>
    public static string GetIconName(this GDTypeFlowNode node)
    {
        return node.Kind switch
        {
            GDTypeFlowNodeKind.Parameter => "MemberSignal",
            GDTypeFlowNodeKind.LocalVariable => "MemberProperty",
            GDTypeFlowNodeKind.MemberVariable => "MemberProperty",
            GDTypeFlowNodeKind.MethodCall => "MemberMethod",
            GDTypeFlowNodeKind.ReturnValue => "ArrowRight",
            GDTypeFlowNodeKind.Assignment => "Edit",
            GDTypeFlowNodeKind.TypeAnnotation => "ClassList",
            GDTypeFlowNodeKind.InheritedMember => "Node",
            GDTypeFlowNodeKind.BuiltinType => "Object",
            GDTypeFlowNodeKind.Literal => "String",
            GDTypeFlowNodeKind.IndexerAccess => "ArrayMesh",
            GDTypeFlowNodeKind.PropertyAccess => "MemberProperty",
            GDTypeFlowNodeKind.TypeCheck => "ClassList",
            GDTypeFlowNodeKind.NullCheck => "GuiRadioUnchecked",
            GDTypeFlowNodeKind.Comparison => "Compare",
            _ => "StatusWarning"
        };
    }
}
