namespace GDShrapt.Abstractions
{
    public sealed class GDCollisionLayerInfo
    {
        public string TypeName { get; }
        public int LayerValue { get; }
        public int MaskValue { get; }
        public string ScenePath { get; }

        public GDCollisionLayerInfo(string typeName, int layerValue, int maskValue, string scenePath)
        {
            TypeName = typeName;
            LayerValue = layerValue;
            MaskValue = maskValue;
            ScenePath = scenePath;
        }
    }
}
