using Microsoft.Xna.Framework;
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

    public class CliffConnectionPoint
    {
        public int Index { get; set; }
        public Vector2 CoordinateOffset { get; set; }
        public byte ConnectsTo { get; set; }
        // Swap the first and last 4 bits to then and then with another point to get the directions they can connect
        public byte ReversedConnectsTo => (byte)((ConnectsTo >> 4) + (0b11110000 & (ConnectsTo << 4)));
        public CliffSide Side { get; set; }

        public List<CliffAStarNode> ConnectTo(CliffAStarNode node, CliffTile tile)
        {
            var possibleNeighbors = tile.ConnectionPoints.Select(cp =>
            {
                (CliffConnectionPoint cp, List<Direction> dirs) connection = (cp, GetDirectionsInMask((byte)(cp.ReversedConnectsTo & ConnectsTo)));
                return connection;
            }).Where(connection => connection.dirs.Count > 0).ToList();

            var neighbors = new List<CliffAStarNode>();
            foreach (var neighbor in possibleNeighbors)
            {
                if (neighbor.cp.Side != node.Exit.Side)
                    continue;

                foreach (Direction dir in neighbor.dirs)
                {
                    Vector2 placementOffset = (Vector2)Helpers.VisualDirectionToPoint(dir) - neighbor.cp.CoordinateOffset;
                    Vector2 placementCoords = node.ExitCoords + placementOffset;

                    var exit = tile.GetExit(neighbor.cp.Index);
                    exit = (CliffConnectionPoint)exit.MemberwiseClone();

                    var newNode = new CliffAStarNode(node, neighbor.cp, exit, placementCoords, tile);

                    // Make sure that the new node doesn't overlap anything
                    if (newNode.OccupiedTiles.Count - node.OccupiedTiles.Count == newNode.Tile.Foundation.Count)
                        neighbors.Add(newNode);
                }
            }
            return neighbors;
        }

        public List<CliffAStarNode> GetConnections(CliffAStarNode node, List<CliffTile> tiles)
        {
            return tiles.SelectMany(tile => ConnectTo(node, tile)).ToList();
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

    public class CliffAStarNode
    {
        private CliffAStarNode() {}

        public CliffAStarNode(CliffAStarNode parent, CliffConnectionPoint entry, CliffConnectionPoint exit, Vector2 location, CliffTile tile)
        {
            Location = location;
            Tile = tile;

            Parent = parent;
            Entry = entry;
            Exit = exit;
            Destination = Parent.Destination;

            OccupiedTiles = new HashSet<Vector2>(parent.OccupiedTiles);
            OccupiedTiles.UnionWith(tile.Foundation.Select(coordinate => coordinate + Location));
        }

        public static CliffAStarNode MakeStartNode(Vector2 location, Vector2 destination, CliffSide startingSide)
        {
            CliffConnectionPoint connectionPoint = new CliffConnectionPoint
            {
                ConnectsTo = 0b11111111,
                CoordinateOffset = new Vector2(0, 0),
                Side = startingSide
            };

            var startNode = new CliffAStarNode()
            {
                Location = location,
                Tile = null,

                Parent = null,
                Entry = null,
                Exit = connectionPoint,
                Destination = destination
            };

            return startNode;
        }

        public List<CliffAStarNode> GetNeighbors(List<CliffTile> tiles)
        {
            var neighbors = Exit.GetConnections(this, tiles);
            return neighbors;
        }

        ///// Tile Config

        /// <summary>
        /// Absolute world coordinates of the tile
        /// </summary>
        public Vector2 Location;

        /// <summary>
        /// Absolute world coordinates of the tile's exit
        /// </summary>
        public Vector2 ExitCoords => Location + Exit.CoordinateOffset;

        /// <summary>
        /// Tile Data
        /// </summary>
        public CliffTile Tile;

        ///// A* Stuff

        /// <summary>
        /// A* end point
        /// </summary>
        public Vector2 Destination;

        /// <summary>
        /// Where this tile connects to the previous tile
        /// </summary>
        public CliffConnectionPoint Entry;

        /// <summary>
        /// Where this tile connects to the next tile
        /// </summary>
        public CliffConnectionPoint Exit;

        // Distance from starting node
        public float GScore => Parent == null ? 0 : Parent.GScore + Helpers.VectorDistance(Parent.ExitCoords, ExitCoords);

        // Distance to end node
        public float HScore => Helpers.VectorDistance(Destination, ExitCoords);
        public float FScore => GScore * 0.8f + HScore;
        public CliffAStarNode Parent;

        public HashSet<Vector2> OccupiedTiles = new HashSet<Vector2>();
    }

    public class CliffTile
    {
        public CliffTile(IniSection iniSection, int index)
        {
            Index = index;

            string indicesString = iniSection.GetStringValue("TileIndices", null);
            if (indicesString == null || !Regex.IsMatch(indicesString, "^((?:\\d+?,)*(?:\\d+?))$"))
                throw new INIConfigException($"Cliff {iniSection.SectionName} has invalid TileIndices list: {indicesString}!");


            string tileSet = iniSection.GetStringValue("TileSet", null);
            if (string.IsNullOrWhiteSpace(tileSet))
                throw new INIConfigException($"Cliff {iniSection.SectionName} has no TileSet!");

            TileSet = tileSet;

            IndicesInTileSet = indicesString.Split(',').Select(int.Parse).ToList();

            ConnectionPoints = new List<CliffConnectionPoint>();

            for (int i = 0; i < 2; i++)
            {
                string coordsString = iniSection.GetStringValue($"ConnectionPoint{i}", null);
                if (coordsString == null || !Regex.IsMatch(coordsString, "^\\d+?,\\d+?$"))
                    throw new INIConfigException($"Cliff {iniSection.SectionName} has invalid ConnectionPoint{i} value: {coordsString}!");

                var coordParts = coordsString.Split(',').Select(int.Parse).ToList();
                Vector2 coords = new Vector2(coordParts[0], coordParts[1]);

                string directionsString = iniSection.GetStringValue($"ConnectionPoint{i}.Directions", null);
                if (directionsString == null || directionsString.Length != (int)Direction.Count || Regex.IsMatch(directionsString, "[^01]"))
                    throw new INIConfigException($"Cliff {iniSection.SectionName} has invalid ConnectionPoint{i}.Directions value: {directionsString}!");

                byte directions = Convert.ToByte(directionsString, 2);

                string sideString = iniSection.GetStringValue($"ConnectionPoint{i}.Side", string.Empty);
                CliffSide side = sideString.ToLower() switch
                {
                    "front" => CliffSide.Front,
                    "back" => CliffSide.Back,
                    _ => throw new INIConfigException($"Cliff {iniSection.SectionName} has an invalid ConnectionPoint{i}.Side value: {sideString}!")
                };

                ConnectionPoints.Add(new CliffConnectionPoint
                {
                    Index = i,
                    ConnectsTo = directions,
                    CoordinateOffset = coords,
                    Side = side
                });
            }

            string foundationString = iniSection.GetStringValue("Foundation", string.Empty);
            if (!Regex.IsMatch(foundationString, "^((?:\\d+?,\\d+?\\|)*(?:\\d+?,\\d+?))$"))
                throw new INIConfigException($"Cliff {iniSection.SectionName} has an invalid Foundation: {foundationString}!");

            Foundation = foundationString.Split("|").Select(coordinateString =>
            {
                var coordinateParts = coordinateString.Split(",");
                return new Vector2(int.Parse(coordinateParts[0]), int.Parse(coordinateParts[1]));
            }).ToList();
        }

        /// <summary>
        /// Tile's in-editor index
        /// </summary>
        public int Index { get; set; }
        public string TileSet { get; set; }

        /// <summary>
        /// Indices of tiles relative to the Tile Set
        /// </summary>
        public List<int> IndicesInTileSet { get; set; }
        public List<CliffConnectionPoint> ConnectionPoints { get; set; }
        public List<Vector2> Foundation { get; set; }

        public CliffConnectionPoint GetExit(int entryIndex)
        {
            return ConnectionPoints.FirstOrDefault(cp => cp.Index != entryIndex) ?? ConnectionPoints.First();
        }
    }

    public class CliffType
    {
        public static CliffType FromIniSection(IniFile iniFile, string sectionName)
        {
            IniSection cliffSection = iniFile.GetSection(sectionName);
            if (cliffSection == null)
                return null;

            string cliffName = cliffSection.GetStringValue("Name", null);

            if (string.IsNullOrEmpty(cliffName))
                return null;

            var allowedTheaters = cliffSection.GetListValue("AllowedTheaters", ',', s => s);

            return new CliffType(iniFile, sectionName, cliffName, allowedTheaters);
        }

        private CliffType(IniFile iniFile, string iniName, string name, List<string> allowedTheaters)
        {
            IniName = iniName;
            Name = name;
            AllowedTheaters = allowedTheaters;

            Tiles = new List<CliffTile>();

            foreach (var sectionName in iniFile.GetSections())
            {
                var parts = sectionName.Split('.');
                if (parts.Length != 2 || parts[0] != IniName || !int.TryParse(parts[1], out int index))
                    continue;

                Tiles.Add(new CliffTile(iniFile.GetSection(sectionName), index));
            }
        }

        public string IniName { get; set; }
        public string Name { get; set; }
        public List<string> AllowedTheaters { get; set; }
        public List<CliffTile> Tiles { get; set; }

    }
}
