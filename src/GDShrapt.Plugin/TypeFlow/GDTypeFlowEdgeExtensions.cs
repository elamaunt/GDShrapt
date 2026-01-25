using GDShrapt.CLI.Core;

namespace GDShrapt.Plugin;

/// <summary>
/// Extension methods for GDTypeFlowEdge to provide Godot-specific UI functionality.
/// </summary>
internal static class GDTypeFlowEdgeExtensions
{
    /// <summary>
    /// Gets the color for this edge based on its kind.
    /// </summary>
    public static Color GetEdgeColor(this GDTypeFlowEdge edge)
    {
        return edge.Kind switch
        {
            GDTypeFlowEdgeKind.TypeFlow => new Color(0.5f, 0.7f, 0.5f),        // Зелёный
            GDTypeFlowEdgeKind.Assignment => new Color(0.5f, 0.7f, 0.9f),      // Синий
            GDTypeFlowEdgeKind.UnionMember => new Color(0.7f, 0.5f, 0.9f),     // Фиолетовый
            GDTypeFlowEdgeKind.DuckConstraint => new Color(1.0f, 0.85f, 0.3f), // Жёлтый
            GDTypeFlowEdgeKind.Return => new Color(1.0f, 0.7f, 0.4f),          // Оранжевый
            _ => new Color(0.5f, 0.5f, 0.5f)
        };
    }
}
