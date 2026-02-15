namespace GDShrapt.Abstractions
{
    public sealed class GDAvoidanceLayerInfo
    {
        public string TypeName { get; }
        public int LayersValue { get; }
        public int MaskValue { get; }
        public string ScenePath { get; }

        public GDAvoidanceLayerInfo(string typeName, int layersValue, int maskValue, string scenePath)
        {
            TypeName = typeName;
            LayersValue = layersValue;
            MaskValue = maskValue;
            ScenePath = scenePath;
        }
    }
}
