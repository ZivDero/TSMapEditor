using Microsoft.Xna.Framework;
using Rampastring.Tools;

namespace TSMapEditor.Models
{
    public class HouseType : GameObjectType
    {
        public HouseType(string iniName) : base(iniName)
        {
        }

        public HouseType(HouseType baseHouseType, string iniName) : base(iniName)
        {
            foreach (var property in typeof(HouseType).GetProperties())
            {
                if (property.CanWrite)
                    property.SetValue(this, property.GetValue(baseHouseType));
            }
        }

        public override RTTIType WhatAmI() => RTTIType.HouseType;

        public string ParentCountry { get; set; }
        public string Suffix { get; set; }
        public string Prefix { get; set; }
        public string Color { get; set; }
        public string Side { get; set; }

        public bool? SmartAI { get; set; }
        public bool? Multiplay { get; set; }
        public bool? MultiplayPassive { get; set; }
        public bool? WallOwner { get; set; }

        public float? ROF { get; set; }
        public float? Firepower { get; set; }

        public float? ArmorInfantryMult { get; set; }
        public float? ArmorUnitsMult { get; set; }
        public float? ArmorAircraftMult { get; set; }
        public float? ArmorBuildingsMult { get; set; }
        public float? ArmorDefensesMult { get; set; }

        public float? CostInfantryMult { get; set; }
        public float? CostUnitsMult { get; set; }
        public float? CostAircraftMult { get; set; }
        public float? CostBuildingsMult { get; set; }
        public float? CostDefensesMult { get; set; }

        public float? SpeedInfantryMult { get; set; }
        public float? SpeedUnitsMult { get; set; }
        public float? SpeedAircraftMult { get; set; }

        public float? BuildtimeInfantryMult { get; set; }
        public float? BuildtimeUnitsMult { get; set; }
        public float? BuildtimeAircraftMult { get; set; }
        public float? BuildtimeBuildingsMult { get; set; }
        public float? BuildtimeDefensesMult { get; set; }

        public float? IncomeMult { get; set; }

        [INI(false)] public Color XNAColor { get; set; } = Microsoft.Xna.Framework.Color.Gray;
        [INI(false)] public bool IsSpawnHouseType { get; set; } = false;

        public void Reset(HouseType baseHouseType)
        {
            foreach (var property in typeof(HouseType).GetProperties())
            {
                if (property.CanWrite)
                    property.SetValue(this, property.GetValue(baseHouseType));
            }
        }

        public void ReadFromIniSection(IniSection iniSection)
        {
            ReadPropertiesFromIniSection(iniSection);
        }

        public void WriteToIniSection(IniSection iniSection)
        {
            WritePropertiesToIniSection(iniSection);
        }
        public bool HasDarkHouseColor() => XNAColor.R < 32 && XNAColor.G < 32 && XNAColor.B < 64;

    }
}
