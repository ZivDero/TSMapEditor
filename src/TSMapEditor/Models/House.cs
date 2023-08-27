using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;

namespace TSMapEditor.Models
{
    public class BaseNode : IPositioned
    {
        public BaseNode()
        {
        }

        public BaseNode(string structureTypeName, Point2D location)
        {
            StructureTypeName = structureTypeName;
            Position = location;
        }

        public string StructureTypeName { get; set; }
        public Point2D Position { get; set; }

        public static BaseNode FromIniString(string iniString)
        {
            if (string.IsNullOrWhiteSpace(iniString))
            {
                Logger.Log($"{nameof(BaseNode)}.{nameof(FromIniString)}: null string or whitespace given as parameter");
                return null;
            }
                
            string[] parts = iniString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                Logger.Log($"{nameof(BaseNode)}.{nameof(FromIniString)}: invalid string " + iniString);
                return null;
            }

            int x = Conversions.IntFromString(parts[1], -1);
            int y = Conversions.IntFromString(parts[2], -1);
            if (x < 0 || y < 0)
            {
                Logger.Log($"{nameof(BaseNode)}.{nameof(FromIniString)}: invalid coordinates given in string " + iniString);
                return null;
            }

            return new BaseNode(parts[0], new Point2D(x, y));
        }
    }

    public class House : AbstractObject
    {
        private const int MaxBaseNodeCount = 1000;

        public override RTTIType WhatAmI() => RTTIType.House;

        public House(string iniName)
        {
            ININame = iniName;
        }

        [INI(false)]
        public string ININame { get; set; }
        public int IQ { get; set; }
        public string Edge { get; set; }
        public string Color { get; set; } = "Grey";
        public string Allies { get; set; }
        public int Credits { get; set; }
        public string Country { get; set; } // This is for YR
        public int? ActsLike { get; set; } // This is for TS
        public int TechLevel { get; set; }
        public int PercentBuilt { get; set; }
        public bool PlayerControl { get; set; }
        public string Side { get; set; } // Only in TS

        [INI(false)] public HouseType HouseType { get; set; }
        [INI(false)] public bool IsPlayerHouse { get; set; } = false;
        [INI(false)] public Color XNAColor { get; set; } = Microsoft.Xna.Framework.Color.Gray;

        public List<BaseNode> BaseNodes { get; } = new List<BaseNode>();
        public void CopyFromOtherHouse(House baseHouse)
        {
            foreach (var property in typeof(House).GetProperties())
            {
                if (property.Name != "BaseNodes" && property.Name != "HouseType")
                    property.SetValue(this, property.GetValue(baseHouse));
            }
        }

        public void ReadFromIniSection(IniSection iniSection)
        {
            ReadPropertiesFromIniSection(iniSection);

            // Read base nodes
            for (int i = 0; i < MaxBaseNodeCount; i++)
            {
                string nodeInfo = iniSection.GetStringValue(i.ToString("D3"), null);
                if (nodeInfo == null)
                    return;

                var baseNode = BaseNode.FromIniString(nodeInfo);
                if (baseNode != null)
                    BaseNodes.Add(baseNode);
            }
        }

        public void WriteToIniSection(IniSection iniSection)
        {
            if (ININame.StartsWith("Spawn") && ININame.Length <= 6)
            {
                iniSection.SetStringValue("Color", Color);
                return;
            }

            WritePropertiesToIniSection(iniSection);

            // Write base nodes
            // Format: Index=BuildingTypeName,X,Y
            // Index is from 000 to 999

            iniSection.SetIntValue("NodeCount", BaseNodes.Count);
            for (int i = 0; i < BaseNodes.Count; i++)
            {
                var node = BaseNodes[i];

                iniSection.SetStringValue(i.ToString("D3"), $"{node.StructureTypeName},{node.Position.X},{node.Position.Y}");
            }

            // Erase potential removed nodes
            for (int i = BaseNodes.Count; i < MaxBaseNodeCount; i++)
            {
                iniSection.RemoveKey(i.ToString("D3"));
            }
        }

        public bool HasDarkHouseColor() => XNAColor.R < 32 && XNAColor.G < 32 && XNAColor.B < 64;
    }
}
