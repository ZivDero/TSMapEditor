using Rampastring.Tools;

namespace TSMapEditor.Models.ArtConfig
{
    public class AircraftArtConfig : IArtConfig
    {
        public bool Voxel { get; set; }
        public bool Remapable => true;
        public int Facings { get; set; } = 8;

        public void ReadFromIniSection(IniSection iniSection)
        {
            if (iniSection == null)
                return;

            Voxel = iniSection.GetBooleanValue(nameof(Voxel), Voxel);
            Facings = iniSection.GetIntValue(nameof(Facings), Facings);
        }
    }
}