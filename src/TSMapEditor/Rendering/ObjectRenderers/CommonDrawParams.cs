namespace TSMapEditor.Rendering.ObjectRenderers
{
    public readonly ref struct CommonDrawParams
    {
        public string IniName { get; init; }
        public ShapeImage MainImage { get; init; }
        public VoxelModel MainModel { get; init; }
        public VoxelModel TurretModel { get; init; }
        public VoxelModel BarrelModel { get; init; }
    }
}
