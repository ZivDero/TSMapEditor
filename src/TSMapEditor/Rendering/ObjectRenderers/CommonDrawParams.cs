namespace TSMapEditor.Rendering.ObjectRenderers
{
    public abstract class CommonDrawParams
    {
        public string IniName;
    }

    public class VoxelDrawParams : CommonDrawParams
    {
        public VoxelModel Graphics;

        public VoxelDrawParams(VoxelModel graphics, string iniName)
        {
            Graphics = graphics;
            IniName = iniName;
        }
    }

    public class ShapeDrawParams : CommonDrawParams
    {
        public ShapeImage Graphics;

        public ShapeDrawParams(ShapeImage graphics, string iniName)
        {
            Graphics = graphics;
            IniName = iniName;
        }
    }
}
