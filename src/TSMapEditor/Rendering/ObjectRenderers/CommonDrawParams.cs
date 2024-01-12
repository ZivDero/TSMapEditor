namespace TSMapEditor.Rendering.ObjectRenderers
{
    public struct CommonDrawParams
    {
        public IDrawableObject Graphics;
        public string IniName;

        public CommonDrawParams(IDrawableObject graphics, string iniName)
        {
            Graphics = graphics;
            IniName = iniName;
        }
    }
}
