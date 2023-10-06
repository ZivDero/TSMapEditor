﻿using CNCMaps.FileFormats.Encodings;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.MapFormat;
using TSMapEditor.Rendering;

namespace TSMapEditor.Initialization
{
    /// <summary>
    /// Contains functions for parsing and applying different sections of a map file.
    /// </summary>
    public static class MapLoader
    {
        private const int BUILDING_PROPERTY_FIELD_COUNT = 17;
        private const int UNIT_PROPERTY_FIELD_COUNT = 14;
        private const int INFANTRY_PROPERTY_FIELD_COUNT = 14;
        private const int AIRCRAFT_PROPERTY_FIELD_COUNT = 12;
        private const int AI_TRIGGER_PROPERTY_FIELD_COUNT = 18;

        public static List<string> MapLoadErrors = new List<string>();

        private static void AddMapLoadError(string error)
        {
            Logger.Log(error);
            MapLoadErrors.Add(error);
        }

        public static void PreCheckMapIni(IniFile mapIni)
        {
            var section = mapIni.GetSection("Map");
            if (section == null)
                throw new MapLoadException("[Map] does not exist in the loaded file!");

            string size = section.GetStringValue("Size", null);
            if (size == null)
                throw new MapLoadException("Invalid [Map] Size=");
            string[] parts = size.Split(',');
            if (parts.Length != 4)
                throw new MapLoadException("Invalid [Map] Size=");

            int width = int.Parse(parts[2], CultureInfo.InvariantCulture);
            int height = int.Parse(parts[3], CultureInfo.InvariantCulture);

            if (width > Constants.MaxMapWidth)
            {
                throw new MapLoadException($"Map width cannot be greater than " +
                    $"{Constants.MaxMapWidth} cells; the map is {width} cells wide!");
            }

            if (height > Constants.MaxMapHeight)
            {
                throw new MapLoadException("Map height cannot be greater than " +
                    $"{Constants.MaxMapHeight} cells; the map is {height} cells high!");
            }
        }

        public static void PostCheckMap(IMap map, TheaterGraphics theaterGraphics)
        {
            map.DoForAllValidTiles(t =>
            {
                if (t.TileIndex >= theaterGraphics.TileCount)
                {
                    AddMapLoadError($"Invalid tile index {t.TileIndex} for cell at {t.CoordsToPoint()} - setting it to 0");
                    t.TileIndex = 0;
                    t.SubTileIndex = 0;
                    return;
                }

                var tile = theaterGraphics.GetTile(t.TileIndex);
                var tileSet = theaterGraphics.Theater.TileSets[tile.TileSetId];
                int maxSubTileIndex = tile.SubTileCount - 1;
                if (t.SubTileIndex > maxSubTileIndex)
                {
                    AddMapLoadError($"Invalid sub-tile index {t.SubTileIndex} for cell at {t.CoordsToPoint()} (max: {maxSubTileIndex}) - setting it to 0. " +
                        $"TileSet: {tileSet.SetName} ({tileSet.FileName}), index of tile within its set: {tile.TileIndexInTileSet}");

                    t.SubTileIndex = 0;

                    if (maxSubTileIndex < 0)
                    {
                        AddMapLoadError($"    Maximum sub-tile count of 0 detected for tile at {t.CoordsToPoint()}, also setting the cell's tile index to 0.");
                        t.TileIndex = 0;
                    }

                    return;
                }

                if (tile.GetSubTile(t.SubTileIndex).TmpImage == null)
                {
                    AddMapLoadError($"Null sub-tile {t.SubTileIndex} for cell at {t.CoordsToPoint()} - clearing the tile. " +
                        $"TileSet: {tileSet.SetName} ({tileSet.FileName}), index of tile within its set: {tile.TileIndexInTileSet}");

                    t.ChangeTileIndex(0, 0);
                }
            });
        }

        public static void ReadMapSection(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("Map");
            if (section == null)
                throw new MapLoadException("[Map] does not exist in the loaded file!");

            string size = section.GetStringValue("Size", null);
            string[] parts = size.Split(',');

            int width = int.Parse(parts[2], CultureInfo.InvariantCulture);
            int height = int.Parse(parts[3], CultureInfo.InvariantCulture);
            map.Size = new Point2D(width, height);

            string localSize = section.GetStringValue("LocalSize", null);
            if (localSize == null)
                throw new MapLoadException("Invalid [Map] LocalSize=");
            parts = localSize.Split(',');
            if (parts.Length != 4)
                throw new MapLoadException("Invalid [Map] LocalSize=");

            map.LocalSize = new Rectangle(
                Conversions.IntFromString(parts[0], 0),
                Conversions.IntFromString(parts[1], 0),
                Conversions.IntFromString(parts[2], width),
                Conversions.IntFromString(parts[3], height));

            map.TheaterName = section.GetStringValue("Theater", string.Empty);
        }

        public static void ReadIsoMapPack(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("IsoMapPack5");
            if (section == null)
                return;

            if (section.Keys.Count == 0)
            {
                Logger.Log("[IsoMapPack5] has no data!");
                map.SetTileData(new List<MapTile>(0));
                return;
            }

            StringBuilder sb = new StringBuilder();
            section.Keys.ForEach(kvp => sb.Append(kvp.Value));

            byte[] compressedData = Convert.FromBase64String(sb.ToString());
            if (compressedData.Length < 4)
                throw new InvalidOperationException("Invalid IsoMapPack5 format");

            Logger.Log("IsoMapPack5 CompressedData length: " + compressedData.Length);

            List<byte> uncompressedData = new List<byte>();

            int position = 0;

            while (position < compressedData.Length)
            {
                ushort inputSize = BitConverter.ToUInt16(compressedData, position);
                ushort outputSize = BitConverter.ToUInt16(compressedData, position + 2);

                Logger.Log("Decoding IsoMapPack5 block: pos: " + position + ", inSize: " + inputSize + ", outSize: " + outputSize);

                if (position + inputSize + 4 > compressedData.Length)
                    throw new InvalidOperationException("Error decoding IsoMapPack5");

                byte[] inData = new byte[inputSize];
                Array.Copy(compressedData, position + 4, inData, 0, inputSize);
                byte[] outData = new byte[outputSize];
                MiniLZO.MiniLZO.Decompress(inData, outData);
                uncompressedData.AddRange(outData);

                position += inputSize + 4;
            }

            // if (uncompressedData.Count % IsoMapPack5Tile.Size != 0)
            //     throw new InvalidOperationException("Decompressed IsoMapPack5 size does not match expected struct size");

            var tiles = new List<MapTile>(uncompressedData.Count % IsoMapPack5Tile.Size);
            position = 0;
            while (position < uncompressedData.Count - IsoMapPack5Tile.Size)
            {
                var mapTile = new MapTile(uncompressedData.GetRange(position, IsoMapPack5Tile.Size).ToArray());
                if (mapTile.TileIndex == ushort.MaxValue)
                {
                    mapTile.TileIndex = 0;
                }
                tiles.Add(mapTile);
                position += IsoMapPack5Tile.Size;
            }

            map.SetTileData(tiles);
        }

        public static void ReadBasicSection(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("Basic");
            if (section == null)
                return;

            map.Basic.ReadPropertiesFromIniSection(section);
        }

        public static void ReadTerrainObjects(IMap map, IniFile mapIni)
        {
            IniSection section = mapIni.GetSection("Terrain");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                string coords = kvp.Key;
                int yLength = coords.Length - 3;
                int y = Conversions.IntFromString(coords.Substring(0, yLength), -1);
                int x = Conversions.IntFromString(coords.Substring(yLength), -1);
                if (y < 0 || x < 0)
                    continue;

                TerrainType terrainType = map.Rules.TerrainTypes.Find(tt => tt.ININame == kvp.Value);
                if (terrainType == null)
                {
                    AddMapLoadError($"Skipping loading of terrain type {kvp.Value}, placed at {x}, {y}, because it does not exist in Rules.");
                    continue;
                }

                var terrainObject = new TerrainObject(terrainType, new Point2D(x, y));
                var tile = map.GetTile(x, y);
                if (tile == null)
                {
                    AddMapLoadError($"Terrain object {terrainType.ININame} has been placed outside of the valid map area, at {x}, {y}. Skipping placing it on the map.");
                    continue;
                }

                map.TerrainObjects.Add(terrainObject);
                tile.TerrainObject = terrainObject;
            }
        }

        private static void FindAttachedTag(IMap map, TechnoBase techno, string attachedTagString)
        {
            if (attachedTagString != Constants.NoneValue1 && attachedTagString != Constants.NoneValue2)
            {
                Tag tag = map.Tags.Find(t => t.ID == attachedTagString);
                if (tag == null)
                {
                    AddMapLoadError($"Unable to find tag {attachedTagString} attached to {techno.WhatAmI()} at {techno.Position}");
                    return;
                }

                techno.AttachedTag = tag;
            }
        }

        public static void ReadBuildings(IMap map, IniFile mapIni)
        {
            IniSection section = mapIni.GetSection("Structures");
            if (section == null)
                return;

            // [Structures]
            // INDEX=OWNER,ID,HEALTH,X,Y,FACING,TAG,AI_SELLABLE,AI_REBUILDABLE,POWERED_ON,UPGRADES,SPOTLIGHT,UPGRADE_1,UPGRADE_2,UPGRADE_3,AI_REPAIRABLE,NOMINAL

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < BUILDING_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string buildingTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[5], Constants.FacingMax)));
                string attachedTag = values[6];
                bool aiSellable = Conversions.BooleanFromString(values[7], true);
                // AI_REBUILDABLE is a leftover
                bool powered = Conversions.BooleanFromString(values[9], true);
                int upgradeCount = Conversions.IntFromString(values[10], 0);
                int spotlight = Conversions.IntFromString(values[11], 0);
                string[] upgradeIds = new string[] { values[12], values[13], values[14] };
                bool aiRepairable = Conversions.BooleanFromString(values[15], false);
                bool nominal = Conversions.BooleanFromString(values[16], false);

                var buildingType = map.Rules.BuildingTypes.Find(bt => bt.ININame == buildingTypeId);
                if (buildingType == null)
                {
                    AddMapLoadError($"Unable to find building type {buildingTypeId} - skipping adding it to map.");
                    continue;
                }

                var building = new Structure(buildingType)
                {
                    HP = health,
                    Position = new Point2D(x, y),
                    Facing = (byte)facing,
                    AISellable = aiSellable,
                    Powered = powered,
                    Spotlight = (SpotlightType)spotlight,
                    AIRepairable = aiRepairable,
                    Nominal = nominal,
                    Owner = map.FindHouse(ownerName)
            };

                if (upgradeCount > 0)
                {
                    int appliedUpgrades = 0;

                    for (int i = 0; i < Structure.MaxUpgradeCount; i++)
                    {
                        if (!Helpers.IsStringNoneValue(upgradeIds[i]))
                        {
                            var upgradeBuildingType = map.Rules.BuildingTypes.Find(b => b.ININame == upgradeIds[i]);
                            if (upgradeBuildingType == null)
                            {
                                AddMapLoadError($"Invalid building upgrade specified for building {buildingTypeId}: " + upgradeIds[i]);
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(upgradeBuildingType.PowersUpBuilding) || !upgradeBuildingType.PowersUpBuilding.Equals(buildingType.ININame, StringComparison.OrdinalIgnoreCase))
                            {
                                AddMapLoadError($"Building {buildingTypeId} has an upgrade {upgradeBuildingType.ININame}, but \r\n{upgradeBuildingType.ININame} " +
                                    $"does not specify {buildingTypeId} in its PowersUpBuilding= key. Skipping adding upgrade to map.");
                                continue;
                            }

                            building.Upgrades[appliedUpgrades] = upgradeBuildingType;
                            appliedUpgrades++;
                        }
                    }
                }

                FindAttachedTag(map, building, attachedTag);

                bool isClear = true;

                void CheckFoundationCell(Point2D cellCoords)
                {
                    if (!isClear)
                        return;

                    var tile = map.GetTile(cellCoords);
                    if (tile == null)
                    {
                        isClear = false;
                        AddMapLoadError($"Building {buildingType.ININame} has been placed outside of the map at {cellCoords}. Skipping adding it to map.");
                        return;
                    }

                    if (tile.Structures.Count > 0)
                    {
                        Logger.Log($"NOTE: Building {buildingType.ININame} exists in the cell at {cellCoords} that already contains other buildings: {string.Join(", ", tile.Structures.Select(s => s.ObjectType.ININame))}");
                    }
                }

                buildingType.ArtConfig.DoForFoundationCoordsOrOrigin(offset => CheckFoundationCell(building.Position + offset));

                if (!isClear)
                    continue;

                map.Structures.Add(building);
                buildingType.ArtConfig.DoForFoundationCoordsOrOrigin(offset =>
                {
                    var tile = map.GetTile(building.Position + offset);
                    tile.Structures.Add(building);
                });
            }
        }

        public static void ReadAircraft(IMap map, IniFile mapIni)
        {
            IniSection section = mapIni.GetSection("Aircraft");
            if (section == null)
                return;

            // [Aircraft]
            // INDEX=OWNER,ID,HEALTH,X,Y,FACING,MISSION,TAG,VETERANCY,GROUP,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < AIRCRAFT_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string aircraftTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[5], Constants.FacingMax)));
                string mission = values[6];
                string attachedTag = values[7];
                int veterancy = Conversions.IntFromString(values[8], 0);
                int group = Conversions.IntFromString(values[9], 0);
                bool autocreateNoRecruitable = Conversions.BooleanFromString(values[10], false);
                bool autocreateYesRecruitable = Conversions.BooleanFromString(values[11], false);

                var aircraftType = map.Rules.AircraftTypes.Find(ut => ut.ININame == aircraftTypeId);
                if (aircraftType == null)
                {
                    AddMapLoadError($"Unable to find aircraft type {aircraftTypeId} - skipping adding it to map.");
                    continue;
                }

                var aircraft = new Aircraft(aircraftType)
                {
                    HP = health,
                    Position = new Point2D(x, y),
                    Facing = (byte)facing,
                    Mission = mission,
                    Veterancy = veterancy,
                    Group = group,
                    AutocreateNoRecruitable = autocreateNoRecruitable,
                    AutocreateYesRecruitable = autocreateYesRecruitable,
                    Owner = map.FindHouse(ownerName)
                };

                FindAttachedTag(map, aircraft, attachedTag);

                map.Aircraft.Add(aircraft);
                var tile = map.GetTile(x, y);
                if (tile != null)
                    tile.Aircraft.Add(aircraft);
            }
        }

        public static void ReadUnits(IMap map, IniFile mapIni)
        {
            IniSection section = mapIni.GetSection("Units");
            if (section == null)
                return;

            // [Units]
            // INDEX=OWNER,ID,HEALTH,X,Y,FACING,MISSION,TAG,VETERANCY,GROUP,HIGH,FOLLOWS_INDEX,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < UNIT_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string unitTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[5], Constants.FacingMax)));
                string mission = values[6];
                string attachedTag = values[7];
                int veterancy = Conversions.IntFromString(values[8], 0);
                int group = Conversions.IntFromString(values[9], 0);
                bool high = Conversions.BooleanFromString(values[10], false);
                int followsIndex = Conversions.IntFromString(values[11], 0);
                bool autocreateNoRecruitable = Conversions.BooleanFromString(values[12], false);
                bool autocreateYesRecruitable = Conversions.BooleanFromString(values[13], false);

                var unitType = map.Rules.UnitTypes.Find(ut => ut.ININame == unitTypeId);
                if (unitType == null)
                {
                    AddMapLoadError($"Unable to find unit type {unitTypeId} - skipping adding it to map.");
                    continue;
                }

                var unit = new Unit(unitType)
                {
                    HP = health,
                    Position = new Point2D(x, y),
                    Facing = (byte)facing,
                    Mission = mission,
                    Veterancy = veterancy,
                    Group = group,
                    High = high,
                    FollowerID = followsIndex,
                    AutocreateNoRecruitable = autocreateNoRecruitable,
                    AutocreateYesRecruitable = autocreateYesRecruitable,
                    Owner = map.FindHouse(ownerName)
                };

                FindAttachedTag(map, unit, attachedTag);

                map.Units.Add(unit);
                var tile = map.GetTile(x, y);
                if (tile != null)
                    tile.Vehicles.Add(unit);
            }

            // Process follow IDs
            foreach (var unit in map.Units)
            {
                if (unit.FollowerID < 0 || unit.FollowerID >= map.Units.Count)
                    continue;

                unit.FollowerUnit = map.Units[unit.FollowerID];
            }
        }

        public static void ReadInfantry(IMap map, IniFile mapIni)
        {
            IniSection section = mapIni.GetSection("Infantry");
            if (section == null)
                return;

            // [Infantry]
            // INDEX=OWNER,ID,HEALTH,X,Y,SUB_CELL,MISSION,FACING,TAG,VETERANCY,GROUP,HIGH,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < INFANTRY_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string infantryTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                SubCell subCell = (SubCell)Conversions.IntFromString(values[5], 0);
                string mission = values[6];
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[7], Constants.FacingMax)));
                string attachedTag = values[8];
                int veterancy = Conversions.IntFromString(values[9], 0);
                int group = Conversions.IntFromString(values[10], 0);
                bool high = Conversions.BooleanFromString(values[11], false);
                bool autocreateNoRecruitable = Conversions.BooleanFromString(values[12], false);
                bool autocreateYesRecruitable = Conversions.BooleanFromString(values[13], false);

                var infantryType = map.Rules.InfantryTypes.Find(it => it.ININame == infantryTypeId);
                if (infantryType == null)
                {
                    AddMapLoadError($"Unable to find infantry type {infantryTypeId} - skipping adding it to map.");
                    continue;
                }

                var infantry = new Infantry(infantryType)
                {
                    HP = health,
                    Position = new Point2D(x, y), // TODO handle sub-cell in position?
                    Facing = (byte)facing,
                    Veterancy = veterancy,
                    Group = group,
                    High = high,
                    AutocreateNoRecruitable = autocreateNoRecruitable,
                    AutocreateYesRecruitable = autocreateYesRecruitable,
                    SubCell = subCell,
                    Mission = mission,
                    Owner = map.FindHouse(ownerName)
                };

                FindAttachedTag(map, infantry, attachedTag);

                map.Infantry.Add(infantry);
                var tile = map.GetTile(x, y);
                if (tile != null)
                    tile.Infantry[(int)subCell] = infantry;
            }
        }

        public static void ReadSmudges(IMap map, IniFile mapIni)
        {
            var smudgesSection = mapIni.GetSection("Smudge");
            if (smudgesSection == null)
                return;

            foreach (var kvp in smudgesSection.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (values.Length < 3)
                {
                    AddMapLoadError($"Invalid syntax in smudge defined in map: {kvp.Value}");
                    continue;
                }

                string smudgeTypeId = values[0];
                int x = Conversions.IntFromString(values[1], -1);
                int y = Conversions.IntFromString(values[2], -1);
                if (values.Length > 3 && values[3] != "0")
                {
                    AddMapLoadError($"Invalid syntax in smudge at {x},{y}: {kvp.Value}");
                    continue;
                }

                var smudgeType = map.Rules.SmudgeTypes.Find(st => st.ININame == smudgeTypeId);
                if (smudgeType == null)
                {
                    AddMapLoadError($"Cell at {x},{y} contains a smudge '{smudgeTypeId}' that does not exist in Rules.ini. Ignoring it.");
                    continue;
                }

                var cell = map.GetTile(x, y);
                if (cell == null)
                {
                    AddMapLoadError($"Smudge at {x},{y} is placed outside of the map. Ignoring it.");
                    continue;
                }

                cell.Smudge = new Smudge() { SmudgeType = smudgeType, Position = new Point2D(x, y) };
            }
        }

        public static void ReadOverlays(IMap map, IniFile mapIni)
        {
            var overlayPackSection = mapIni.GetSection("OverlayPack");
            var overlayDataPackSection = mapIni.GetSection("OverlayDataPack");
            if (overlayPackSection == null || overlayDataPackSection == null)
                return;

            var stringBuilder = new StringBuilder();
            overlayPackSection.Keys.ForEach(kvp => stringBuilder.Append(kvp.Value));
            byte[] compressedData = Convert.FromBase64String(stringBuilder.ToString());
            byte[] uncompressedOverlayPack = new byte[Constants.MAX_MAP_LENGTH_IN_DIMENSION * Constants.MAX_MAP_LENGTH_IN_DIMENSION];
            Format5.DecodeInto(compressedData, uncompressedOverlayPack, Constants.OverlayPackFormat);

            stringBuilder.Clear();
            overlayDataPackSection.Keys.ForEach(kvp => stringBuilder.Append(kvp.Value));
            compressedData = Convert.FromBase64String(stringBuilder.ToString());
            byte[] uncompressedOverlayDataPack = new byte[Constants.MAX_MAP_LENGTH_IN_DIMENSION * Constants.MAX_MAP_LENGTH_IN_DIMENSION];
            Format5.DecodeInto(compressedData, uncompressedOverlayDataPack, Constants.OverlayPackFormat);

            for (int y = 0; y < map.Tiles.Length; y++)
            {
                for (int x = 0; x < map.Tiles[y].Length; x++)
                {
                    var tile = map.Tiles[y][x];
                    if (tile == null)
                        continue;

                    int overlayDataIndex = (tile.Y * Constants.MAX_MAP_LENGTH_IN_DIMENSION) + tile.X;
                    int overlayTypeIndex = uncompressedOverlayPack[overlayDataIndex];
                    if (overlayTypeIndex == Constants.NO_OVERLAY)
                        continue;

                    if (overlayTypeIndex >= map.Rules.OverlayTypes.Count)
                    {
                        AddMapLoadError("Ignoring overlay on tile at " + x + ", " + y + " because it's out of bounds compared to Rules.ini overlay list");
                        continue;
                    }

                    var overlayType = map.Rules.OverlayTypes[overlayTypeIndex];
                    var overlay = new Overlay()
                    {
                        OverlayType = overlayType,
                        FrameIndex = uncompressedOverlayDataPack[overlayDataIndex],
                        Position = new Point2D(tile.X, tile.Y)
                    };
                    tile.Overlay = overlay;
                }
            }
        }

        public static void ReadWaypoints(IMap map, IniFile mapIni)
        {
            var waypointsSection = mapIni.GetSection("Waypoints");
            if (waypointsSection == null)
                return;

            foreach (var kvp in waypointsSection.Keys)
            {
                var waypoint = Waypoint.ParseWaypoint(kvp.Key, kvp.Value);
                if (waypoint == null)
                {
                    AddMapLoadError($"Invalid syntax encountered for waypoint: {kvp.Key}={kvp.Value}");
                    continue;
                }

                var tile = map.GetTile(waypoint.Position.X, waypoint.Position.Y);
                if (tile == null)
                {
                    AddMapLoadError($"Waypoint {waypoint.Identifier} at {waypoint.Position} is not within the valid map area.");
                    continue;
                }

                if (tile.Waypoints.Count > 0)
                {
                    Logger.Log($"NOTE: Waypoint {waypoint.Identifier} exists in the cell at {waypoint.Position} that already contains other waypoints: {string.Join(", ", tile.Waypoints.Select(s => s.Identifier))}");
                    continue;
                }

                map.AddWaypoint(waypoint);
            }
        }

        public static void ReadTaskForces(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("TaskForces");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                var taskForce = TaskForce.ParseTaskForce(map.Rules, mapIni.GetSection(kvp.Value));

                map.AddTaskForce(taskForce);
            }
        }

        public static void ReadTriggers(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("Triggers");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                var trigger = Trigger.ParseTrigger(kvp.Key, kvp.Value);
                if (trigger != null)
                    map.AddTrigger(trigger);

                string actionData = mapIni.GetStringValue("Actions", trigger.ID, null);
                trigger.ParseActions(actionData);

                string conditionData = mapIni.GetStringValue("Events", trigger.ID, null);
                trigger.ParseConditions(conditionData);

                trigger.ParseEditorInfo(mapIni);
            }

            // Parse and apply linked triggers
            foreach (var trigger in map.Triggers)
            {
                if (Helpers.IsStringNoneValue(trigger.LinkedTriggerId))
                    continue;

                trigger.LinkedTrigger = map.Triggers.Find(otherTrigger => otherTrigger.ID == trigger.LinkedTriggerId);
            }
        }

        public static void ReadTags(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("Tags");
            if (section == null)
                return;

            // [Tags]
            // ID=REPEATING,NAME,TRIGGER_ID

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                string[] parts = kvp.Value.Split(',');
                if (parts.Length != 3)
                    continue;

                int repeating = Conversions.IntFromString(parts[0], -1);
                if (repeating < 0 || repeating > Tag.REPEAT_TYPE_MAX)
                    continue;

                string triggerId = parts[2];
                Trigger trigger = map.Triggers.Find(t => t.ID == triggerId);
                if (trigger == null)
                {
                    AddMapLoadError("Ignoring tag " + kvp.Key + " because its related trigger " + triggerId + " does not exist!");
                    continue;
                }

                var tag = new Tag() { ID = kvp.Key, Repeating = repeating, Name = parts[1], Trigger = trigger };
                map.AddTag(tag);
            }
        }

        public static void ReadScripts(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("ScriptTypes");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                var script = Script.ParseScript(kvp.Value, mapIni.GetSection(kvp.Value));

                if (script != null)
                    map.AddScript(script);
            }
        }

        public static void ReadTeamTypes(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("TeamTypes");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                var teamTypeSection = mapIni.GetSection(kvp.Value);
                if (teamTypeSection == null)
                    continue;

                var teamType = new TeamType(kvp.Value);
                teamType.ReadPropertiesFromIniSection(teamTypeSection);
                string houseIniName = teamTypeSection.GetStringValue("House", string.Empty);
                string scriptId = teamTypeSection.GetStringValue("Script", string.Empty);
                string taskForceId = teamTypeSection.GetStringValue("TaskForce", string.Empty);
                string tagId = teamTypeSection.GetStringValue("Tag", string.Empty);

                teamType.HouseType = map.FindHouseType(houseIniName);
                teamType.Script = map.Scripts.Find(s => s.ININame == scriptId);
                teamType.TaskForce = map.TaskForces.Find(t => t.ININame == taskForceId);
                teamType.Tag = map.Tags.Find(t => t.ID == tagId);

                if (teamType.HouseType == null)
                {
                    AddMapLoadError($"TeamType {teamType.ININame} has an invalid house ({houseIniName}) specified!");
                }

                if (teamType.Script == null)
                {
                    AddMapLoadError($"TeamType {teamType.ININame} has an invalid script ({scriptId}) specified!");
                }

                if (teamType.TaskForce == null)
                {
                    AddMapLoadError($"TeamType {teamType.ININame} has an invalid TaskForce ({taskForceId}) specified!");
                }

                map.AddTeamType(teamType);
            }
        }

        public static void ReadAITriggerTypes(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("AITriggerTypes");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                string[] parts = kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != AI_TRIGGER_PROPERTY_FIELD_COUNT)
                {
                    AddMapLoadError($"AITriggerType {kvp.Key} is invalid, skipping reading it.");
                    continue;
                }

                var aiTriggerType = new AITriggerType(kvp.Key);

                aiTriggerType.Name = parts[0];
                aiTriggerType.PrimaryTeam = map.TeamTypes.Find(tt => tt.ININame == parts[1]);
                aiTriggerType.OwnerName = parts[2];
                aiTriggerType.Owner = map.FindHouseType(aiTriggerType.OwnerName);

                if (!int.TryParse(parts[3], CultureInfo.InvariantCulture, out int techLevel))
                {
                    AddMapLoadError($"AITriggerType {kvp.Key} has an invalid tech level, skipping parsing of the AI trigger.");
                    continue;
                }
                aiTriggerType.TechLevel = techLevel;

                if (!int.TryParse(parts[4], CultureInfo.InvariantCulture, out int conditionType))
                {
                    AddMapLoadError($"AITriggerType {kvp.Key} has an invalid tech level, skipping parsing of the AI trigger.");
                    continue;
                }

                aiTriggerType.ConditionType = (AITriggerConditionType)conditionType;

                if (!Helpers.IsStringNoneValue(parts[5]))
                {
                    TechnoType conditionObject = map.Rules.FindTechnoType(parts[5]);

                    if (conditionObject == null)
                    {
                        AddMapLoadError($"AITriggerType {kvp.Key} has a non-existent condition object \"{parts[5]}\"");
                    }

                    aiTriggerType.ConditionObjectString = parts[5];
                }

                aiTriggerType.LoadedComparatorString = parts[6];
                AITriggerComparator? comparator = AITriggerComparator.Parse(aiTriggerType.LoadedComparatorString);
                if (comparator == null)
                {
                    AddMapLoadError($"Failed to parse comparator of AITriggerType {kvp.Key} ({aiTriggerType.Name})! Skipping loading of the AI trigger.");
                    continue;
                }
                aiTriggerType.Comparator = comparator.Value;

                aiTriggerType.InitialWeight = Conversions.DoubleFromString(parts[7], 0.0);
                aiTriggerType.MinimumWeight = Conversions.DoubleFromString(parts[8], 0.0);
                aiTriggerType.MaximumWeight = Conversions.DoubleFromString(parts[9], 0.0);
                aiTriggerType.EnabledInMultiplayer = parts[10] != "0";
                aiTriggerType.Unused = parts[11] != "0";
                aiTriggerType.Side = Conversions.IntFromString(parts[12], 0);
                aiTriggerType.IsBaseDefense = parts[13] != "0";

                if (!Helpers.IsStringNoneValue(parts[14]) )
                {
                    aiTriggerType.SecondaryTeam = map.TeamTypes.Find(tt => tt.ININame == parts[14]);

                    if (aiTriggerType.SecondaryTeam == null)
                    {
                        AddMapLoadError($"AITriggerType {kvp.Key} has a non-existent secondary team type \"{parts[14]}\"");
                    }
                }

                aiTriggerType.Easy = parts[15] != "0";
                aiTriggerType.Medium = parts[16] != "0";
                aiTriggerType.Hard = parts[17] != "0";

                map.AITriggerTypes.Add(aiTriggerType);
            }
        }

        public static void ReadHouses(IMap map, IniFile mapIni)
        {
            var housesSection = mapIni.GetSection("Houses");
            if (housesSection == null)
                return;

            // First get all the houses no matter how they're ordered
            var loadedHouses = new List<House>();
            foreach (var kvp in housesSection.Keys)
            {
                string houseName = kvp.Value;

                // For player houses, just read the color
                if (houseName.StartsWith("Spawn") || houseName.StartsWith("<Player @"))
                {
                    var playerHouse = map.PlayerHouses.Find(h => h.ININame == houseName);
                    if (playerHouse != null)
                    {
                        var sec = mapIni.GetSection(houseName);
                        if (sec == null)
                            continue;
                        
                        string color = sec.GetStringValue("Color", "Grey");
                        playerHouse.Color = color;

                        if (map.Rules.Colors.Find(c => c.Name == color) is var xnaColor && xnaColor != null)
                            playerHouse.XNAColor = xnaColor.XNAColor;
                        else
                            playerHouse.XNAColor = Color.Gray;

                        continue;
                    }
                }

                // If we find fake houses, discard them but remember TS Coop mode
                if (houseName.StartsWith("Fake") && houseName.Length <= 6)
                {
                    map.TsCoop = true;
                    continue;
                }

                // Actual houses get saved for now
                var house = new House(houseName);

                loadedHouses.Add(house);

                var houseSection = mapIni.GetSection(houseName);
                if (houseSection != null)
                {
                    house.ReadFromIniSection(houseSection);

                    var color = map.Rules.Colors.Find(c => c.Name == house.Color);
                    if (color == null)
                        house.XNAColor = Color.Black;
                    else 
                        house.XNAColor = color.XNAColor;
                }
            }

            // Now first add the Houses that match the standard ones, and replace the generated ones with them
            var housesToAdd = new List<House>();
            for (int i = 0; i < map.StandardHouses.Count; i++)
            {
                if (loadedHouses.FindIndex(h => h.ININame == map.StandardHouses[i].ININame) is var index && index != -1)
                {
                    map.StandardHouses[i] = loadedHouses[index];
                    housesToAdd.Add(loadedHouses[index]);
                    loadedHouses.RemoveAt(index);
                }
            }

            // Now add the rest to the end
            foreach (var house in loadedHouses)
            {
                housesToAdd.Add(house);
            }

            map.Houses.AddRange(housesToAdd);
        }

        public static void ReadOrMakeHouseTypes(IMap map, IniFile mapIni)
        {
            var loadedHouseTypes = new List<HouseType>();
            if (Constants.UseCountries)
            {
                // YR Countries get loaded from [Countries]
                var section = mapIni.GetSection("Countries");
                if (section == null)
                    return;

                foreach (var kvp in section.Keys)
                {
                    string houseTypeName = kvp.Value;
                    var houseType = new HouseType(houseTypeName);

                    var houseTypeSection = mapIni.GetSection(houseTypeName);
                    if (houseTypeSection != null)
                    {
                        houseType.ReadFromIniSection(houseTypeSection);

                        var color = map.Rules.Colors.Find(c => c.Name == houseType.Color);
                        if (color == null)
                            houseType.XNAColor = Color.Black;
                        else
                            houseType.XNAColor = color.XNAColor;
                    }

                    loadedHouseTypes.Add(houseType);
                }
            }
            else
            {
                // TS Needs the HouseTypes to be generated from the in-map houses. The actual properties of the HouseTypes don't really matter
                foreach (var house in map.Houses)
                {
                    if (map.StandardHouseTypes.Exists(c => c.ININame == house.ININame))
                        continue;

                    var houseType = new HouseType(house.ININame)
                    {
                        Color = house.Color,
                        XNAColor = house.XNAColor,
                        Side = house.Side
                    };
                    
                    loadedHouseTypes.Add(houseType);
                }
            }

            // Now first put in the HouseTypes that match the standard ones, copying their index
            var houseTypesToAdd = new List<HouseType>();
            for (int i = 0; i < map.StandardHouseTypes.Count; i++)
            {
                if (loadedHouseTypes.FindIndex(c => c.ININame == map.StandardHouseTypes[i].ININame) is var index && index != -1)
                {
                    loadedHouseTypes[index].Index = map.StandardHouseTypes[i].Index;
                    map.StandardHouseTypes[i] = loadedHouseTypes[index];
                    houseTypesToAdd.Add(loadedHouseTypes[index]);
                    houseTypesToAdd.RemoveAt(index);
                }
            }

            // Now add the rest to the end, assigning indices after the standard ones
            int houseTypeCount = 0;
            foreach (var houseType in loadedHouseTypes)
            {
                houseTypesToAdd.Add(houseType);
                houseType.Index = map.Rules.HouseTypes.Count + houseTypeCount;
                houseTypeCount++;
            }

            map.HouseTypes.AddRange(houseTypesToAdd);
        }

        public static void ReadCellTags(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("CellTags");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                Point2D? coords = Helpers.CoordStringToPoint(kvp.Key);
                if (coords == null)
                    continue;

                var tile = map.GetTile(coords.Value.X, coords.Value.Y);
                if (tile == null)
                    continue;

                Tag tag = map.Tags.Find(t => t.ID == kvp.Value);
                if (tag == null)
                    continue;

                map.AddCellTag(new CellTag() { Position = coords.Value, Tag = tag });
            }
        }

        public static void ReadLocalVariables(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("VariableNames");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (!int.TryParse(kvp.Key, out int variableIndex))
                {
                    AddMapLoadError($"Invalid local variable index in entry {kvp.Key}: {kvp.Value}, skipping reading local variable.");
                    continue;
                }

                if (map.LocalVariables.Exists(c => c.Index == variableIndex))
                {
                    AddMapLoadError($"Duplicate local variable index in entry {kvp.Key}: {kvp.Value}, skipping reading local variable.");
                    continue;
                }

                string[] parts = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    AddMapLoadError($"Invalid local variable syntax in entry {kvp.Key}: {kvp.Value}, skipping reading local variable.");
                    continue;
                }

                var localVariable = new LocalVariable(variableIndex);
                localVariable.Name = parts[0];
                localVariable.InitialState = int.Parse(parts[1], CultureInfo.InvariantCulture);

                map.LocalVariables.Add(localVariable);
            }
        }

        public static void ReadTubes(IMap map, IniFile mapIni)
        {
            var section = mapIni.GetSection("Tubes");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                // Index=ENTER_X,ENTER_Y,FACING,EXIT_X,EXIT_Y,DIRECTIONS

                string[] parts = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 6)
                    return;

                int enterX = Conversions.IntFromString(parts[0], -1);
                int enterY = Conversions.IntFromString(parts[1], -1);
                TubeDirection initialFacing = (TubeDirection)Conversions.IntFromString(parts[2], -1);
                int exitX = Conversions.IntFromString(parts[3], -1);
                int exitY = Conversions.IntFromString(parts[4], -1);
                var directions = new List<TubeDirection>();
                for (int i = 5; i < parts.Length; i++)
                {
                    directions.Add((TubeDirection)Conversions.IntFromString(parts[i], -1));
                }

                if (enterX < 1 || enterY < 1 || exitX < 1 || exitY < 1 || (int)initialFacing < -1 || initialFacing > TubeDirection.Max)
                {
                    AddMapLoadError("Invalid tube entry: " + kvp.Value);
                    continue;
                }

                var tube = new Tube(new Point2D(enterX, enterY), new Point2D(exitX, exitY), initialFacing, directions);
                map.Tubes.Add(tube);
            }
        }
    }
}
