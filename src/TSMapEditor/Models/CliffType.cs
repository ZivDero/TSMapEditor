using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Models
{
    public enum CliffSide
    {
        Front,
        Back
    }

    public class CliffConnectionPoint
    {
        public Vector2 Coordinates { get; set; }
        public byte ConnectsTo { get; set; }
        // Swap the first and last 4 bits to then and then with another point to get the directions they can connect
        public byte ReversedConnectsTo => (byte)((ConnectsTo >> 4) + (0b11110000 & (ConnectsTo << 4)));

        public List<PotentialCliffPlacement> ConnectTo(CliffTileConfig tile)
        {
            var possibleConnections = tile.ConnectionPoints.Select(cp =>
            {
                (CliffConnectionPoint cp, List<Direction> dirs) connection = (cp, GetDirectionsInMask((byte)(cp.ReversedConnectsTo & ConnectsTo)));
                return connection;
            }).Where(connection => connection.dirs.Count > 0).ToList();

            var potentialPlacements = new List<PotentialCliffPlacement>();
            foreach (var connection in possibleConnections)
            {
                foreach (Direction dir in connection.dirs)
                {
                    Vector2 placementCoords = Coordinates - connection.cp.Coordinates + (Vector2)Helpers.VisualDirectionToPoint(dir);
                    var nextConnectionPoint = tile.ConnectionPoints.FirstOrDefault(cp => cp.Coordinates != connection.cp.Coordinates) ?? connection.cp;
                    nextConnectionPoint = (CliffConnectionPoint)nextConnectionPoint.MemberwiseClone();
                    nextConnectionPoint.Coordinates += placementCoords;

                    potentialPlacements.Add(new PotentialCliffPlacement
                    {
                        PlacementCoords = placementCoords,
                        NextConnectionPoint = nextConnectionPoint,
                        Tile = tile
                    });
                }
            }
            return potentialPlacements;
        }

        private List<Direction> GetDirectionsInMask(byte mask)
        {
            List <Direction> directions = new List<Direction>();

            for (int direction = 0; direction < (int)Direction.Count; direction++)
            {
                if ((mask & (byte)(0b10000000 >> direction)) > 0)
                    directions.Add((Direction)direction);
            }

            return directions;
        }

    }

    public struct PotentialCliffPlacement
    {
        public Vector2 PlacementCoords;
        public CliffConnectionPoint NextConnectionPoint;
        public CliffTileConfig Tile;
    }

    public class CliffTileConfig
    {
        public int TileIndexInSet { get; set; }
        public List<CliffConnectionPoint> ConnectionPoints { get; set; }
        public CliffSide Side { get; set; }

    }

    public class CliffType
    {
        public CliffType(IniFile iniConfig, string tileSet)
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

                // I was going to allow infinite connection points, but to avoid complications I'm limiting them to 2
                for (int i = 0; i < 2; i++)
                {
                    string coordsString = iniSection.GetStringValue($"ConnectionPoint{i}", null);
                    if (coordsString == null || !Regex.IsMatch(coordsString, "^\\d+?,\\d+?$"))
                        break;

                    var coordParts = coordsString.Split(',').Select(int.Parse).ToList();
                    Vector2 coords = new Vector2(coordParts[0], coordParts[1]);

                    string directionsString = iniSection.GetStringValue($"ConnectionPoint{i}.Directions", null);
                    if (directionsString == null || directionsString.Length != (int)Direction.Count || Regex.IsMatch(directionsString, "[^01]"))
                        break;

                    byte directions = Convert.ToByte(directionsString, 2);

                    connectionPoints.Add(new CliffConnectionPoint()
                    {
                        ConnectsTo = directions,
                        Coordinates = coords
                    });
                }

                Tiles.Add(new CliffTileConfig()
                {
                    ConnectionPoints = connectionPoints,
                    TileIndexInSet = tileIndexInSet,
                    Side = side
                });
            }
        }

        public string TileSet { get; set; }

        public List<CliffTileConfig> Tiles { get; set; }

    }
}
