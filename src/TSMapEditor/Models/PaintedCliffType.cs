using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Models
{
    public enum CliffSide
    {
        Front,
        Back
    }

    public struct CliffConnectionPoint
    {
        public Point2D Coordinates { get; set; }
        public byte ConnectsTo { get; set; }
        public CliffSide Side { get; set; }

    }

    public class CliffTileConfig
    {
        public int TileIndexInSet { get; set; }
        public List<CliffConnectionPoint> ConnectionPoints { get; set; }

    }

    public class PaintedCliffType
    {
        public PaintedCliffType(IniFile iniConfig, string tileSet)
        {
            TileSet = tileSet;
            Tiles = new List<CliffTileConfig>();

            foreach (var sectionName in iniConfig.GetSections())
            {
                var parts = sectionName.Split('.');
                if (parts.Length != 2 || parts[0] != tileSet || !int.TryParse(parts[1], out int tileIndexInSet))
                    continue;

                var iniSection = iniConfig.GetSection(sectionName);
                string sideString = iniSection.GetStringValue("Side", string.Empty);
                CliffSide side = sideString.ToLower() switch
                {
                    "front" => CliffSide.Front,
                    "back" => CliffSide.Back,
                    _ => throw new INIConfigException($"Cliff {sectionName} has an invalid Side {sideString}!")
                };

                List<CliffConnectionPoint> connectionPoints = new List<CliffConnectionPoint>();

                for (int i = 0; true; i++)
                {
                    string coordsString = iniSection.GetStringValue($"ConnectionPoint{i}", null);
                    if (coordsString == null || !Regex.IsMatch(coordsString, "^\\d+?,\\d+?$"))
                        break;

                    var coordParts = coordsString.Split(',').Select(int.Parse).ToList();
                    Point2D coords = new Point2D(coordParts[0], coordParts[1]);

                    string directionsString = iniSection.GetStringValue($"ConnectionPoint{i}.Directions", null);
                    if (directionsString == null || directionsString.Length != (int)Direction.Count || Regex.IsMatch(directionsString, "[^01]"))
                        break;

                    byte directions = Convert.ToByte(directionsString, 2);

                    connectionPoints.Add(new CliffConnectionPoint()
                    {
                        ConnectsTo = directions,
                        Coordinates = coords,
                        Side = side
                    });
                }

                Tiles.Add(new CliffTileConfig()
                {
                    ConnectionPoints = connectionPoints,
                    TileIndexInSet = tileIndexInSet
                });
            }
        }

        public string TileSet { get; set; }

        public List<CliffTileConfig> Tiles { get; set; }

    }
}
