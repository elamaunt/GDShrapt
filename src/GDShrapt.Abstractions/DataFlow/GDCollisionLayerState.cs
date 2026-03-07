namespace GDShrapt.Abstractions;

/// <summary>
/// Immutable collision layer/mask state at a program point.
/// </summary>
public sealed class GDCollisionLayerState
{
    public int LayerValue { get; }
    public int MaskValue { get; }

    public GDCollisionLayerState(int layerValue, int maskValue)
    {
        LayerValue = layerValue;
        MaskValue = maskValue;
    }

    public GDCollisionLayerState WithLayer(int newLayer) => new GDCollisionLayerState(newLayer, MaskValue);
    public GDCollisionLayerState WithMask(int newMask) => new GDCollisionLayerState(LayerValue, newMask);

    public override string ToString() => $"layer={LayerValue}, mask={MaskValue}";
}
