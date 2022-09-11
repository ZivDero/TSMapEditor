﻿using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Initialization;
using TSMapEditor.Rendering;

namespace TSMapEditor.Models
{
    public class HouseEventArgs : EventArgs
    {
        public HouseEventArgs(House house)
        {
            House = house;
        }

        public House House { get; }
    }

    public class Map : IMap
    {
        private const int TileBufferSize = 600; // for now

        public event EventHandler HousesChanged;
        public event EventHandler<HouseEventArgs> HouseColorChanged;
        public event EventHandler LocalSizeChanged;
        public event EventHandler MapResized;
        public event EventHandler MapWritten;

        public IniFile LoadedINI { get; set; }

        public Rules Rules { get; private set; }
        public EditorConfig EditorConfig { get; private set; }

        public BasicSection Basic { get; private set; } = new BasicSection();

        public MapTile[][] Tiles { get; private set; }
        public MapTile GetTile(int x, int y)
        {
            if (y < 0 || y >= Tiles.Length)
                return null;

            if (x < 0 || x >= Tiles[y].Length)
                return null;

            return Tiles[y][x];
        }

        public MapTile GetTile(Point2D cellCoords) => GetTile(cellCoords.X, cellCoords.Y);
        public MapTile GetTileOrFail(Point2D cellCoords) => GetTile(cellCoords.X, cellCoords.Y) ?? throw new InvalidOperationException("Invalid cell coords: " + cellCoords);
        public List<Aircraft> Aircraft { get; private set; } = new List<Aircraft>();
        public List<Infantry> Infantry { get; private set; } = new List<Infantry>();
        public List<Unit> Units { get; private set; } = new List<Unit>();
        public List<Structure> Structures { get; private set; } = new List<Structure>();

        public void DoForAllTechnos(Action<TechnoBase> action)
        {
            Aircraft.ForEach(a => action(a));
            Infantry.ForEach(i => action(i));
            Units.ForEach(u => action(u));
            Structures.ForEach(s => action(s));
        }

        /// <summary>
        /// The list of standard houses loaded from Rules.ini.
        /// Relevant when the map itself has no houses specified.
        /// New houses might be added to this list if the map has
        /// objects whose owner does not exist in the map's list of houses
        /// or in the Rules.ini standard house list.
        /// </summary>
        public List<House> StandardHouses { get; set; }
        public List<House> Houses { get; } = new List<House>();
        public List<House> GetHouses() => Houses.Count > 0 ? Houses : StandardHouses;

        public List<TerrainObject> TerrainObjects { get; private set; } = new List<TerrainObject>();
        public List<Waypoint> Waypoints { get; private set; } = new List<Waypoint>();

        public List<TaskForce> TaskForces { get; } = new List<TaskForce>();
        public List<Trigger> Triggers { get; } = new List<Trigger>();
        public List<Tag> Tags { get; } = new List<Tag>();
        public List<CellTag> CellTags { get; private set; } = new List<CellTag>();
        public List<Script> Scripts { get; } = new List<Script>();
        public List<TeamType> TeamTypes { get; } = new List<TeamType>();
        public List<LocalVariable> LocalVariables { get; } = new List<LocalVariable>();
        public List<Tube> Tubes { get; private set; } = new List<Tube>();

        public Lighting Lighting { get; } = new Lighting();

        public List<GraphicalBaseNode> GraphicalBaseNodes { get; } = new List<GraphicalBaseNode>();

        public Point2D Size { get; set; }

        private Rectangle _localSize;
        public Rectangle LocalSize 
        {
            get => _localSize;
            set
            {
                _localSize = value;
                LocalSizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string TheaterName { get; set; }
        public ITheater TheaterInstance { get; set; }

        private readonly Initializer initializer;


        public Map()
        {
            InitCells();

            initializer = new Initializer(this);
        }

        private void InitEditorConfig()
        {
            EditorConfig = new EditorConfig();
            EditorConfig.Init(Rules);
        }

        public void InitNew(IniFile rulesIni, IniFile firestormIni, IniFile artIni, IniFile artFirestormIni, string theaterName, Point2D size)
        {
            const int marginY = 6;
            const int marginX = 4;

            Initialize(rulesIni, firestormIni, artIni, artFirestormIni);
            LoadedINI = new IniFile();
            var baseMap = new IniFile(Environment.CurrentDirectory + "/Config/BaseMap.ini");
            baseMap.FileName = string.Empty;
            baseMap.SetStringValue("Map", "Theater", theaterName);
            baseMap.SetStringValue("Map", "Size", $"0,0,{size.X},{size.Y}");
            baseMap.SetStringValue("Map", "LocalSize", $"{marginX},{marginY},{size.X - (marginX * 2)},{size.Y - (marginY * 2)}");
            LoadExisting(rulesIni, firestormIni, artIni, artFirestormIni, baseMap);
            SetTileData(null);
        }

        public void LoadExisting(IniFile rulesIni, IniFile firestormIni, IniFile artIni, IniFile artFirestormIni, IniFile mapIni)
        {
            Initialize(rulesIni, firestormIni, artIni, artFirestormIni);

            LoadedINI = mapIni ?? throw new ArgumentNullException(nameof(mapIni));
            Rules.InitFromINI(mapIni, initializer, true);
            InitEditorConfig();

            MapLoader.MapLoadErrors.Clear();

            MapLoader.ReadBasicSection(this, mapIni);
            MapLoader.ReadMapSection(this, mapIni);
            MapLoader.ReadIsoMapPack(this, mapIni);

            MapLoader.ReadHouses(this, mapIni);

            MapLoader.ReadSmudges(this, mapIni);
            MapLoader.ReadOverlays(this, mapIni);
            MapLoader.ReadTerrainObjects(this, mapIni);
            MapLoader.ReadTubes(this, mapIni);

            MapLoader.ReadWaypoints(this, mapIni);
            MapLoader.ReadTaskForces(this, mapIni);
            MapLoader.ReadTriggers(this, mapIni);
            MapLoader.ReadTags(this, mapIni);
            MapLoader.ReadCellTags(this, mapIni);
            MapLoader.ReadScripts(this, mapIni);
            MapLoader.ReadTeamTypes(this, mapIni);
            MapLoader.ReadLocalVariables(this, mapIni);

            MapLoader.ReadBuildings(this, mapIni);
            MapLoader.ReadAircraft(this, mapIni);
            MapLoader.ReadUnits(this, mapIni);
            MapLoader.ReadInfantry(this, mapIni);

            // Check base nodes and create graphical base node instances from them
            if (Houses.Count > 0)
            {
                foreach (var house in Houses)
                {
                    for (int i = 0; i < house.BaseNodes.Count; i++)
                    {
                        var baseNode = house.BaseNodes[i];

                        BuildingType buildingType = Rules.BuildingTypes.Find(bt => bt.ININame == baseNode.StructureTypeName);
                        bool remove = false;
                        if (buildingType == null)
                        {
                            Logger.Log($"Building type {baseNode.StructureTypeName} not found for base node for house {house.ININame}! Removing the node.");
                            remove = true;
                        }
                        
                        var cell = GetTile(baseNode.Location);
                        if (cell == null)
                        {
                            Logger.Log($"Base node for building type {baseNode.StructureTypeName} for house {house.ININame} is outside of the map! Coords: {baseNode.Location}. Removing the node.");
                            remove = true;
                        }

                        if (remove)
                        {
                            house.BaseNodes.RemoveAt(i);
                            i--;
                            continue;
                        }

                        var graphicalBaseNode = new GraphicalBaseNode(baseNode, buildingType, house);
                        GraphicalBaseNodes.Add(graphicalBaseNode);
                    }
                }
            }

            Lighting.ReadFromIniFile(mapIni);
        }

        public void Write()
        {
            LoadedINI.Comment = "Written by DTA Scenario Editor\r\n; all comments have been truncated\r\n; www.moddb.com/members/Rampastring\r\n; github.com/Rampastring";

            MapWriter.WriteMapSection(this, LoadedINI);
            MapWriter.WriteBasicSection(this, LoadedINI);
            MapWriter.WriteIsoMapPack5(this, LoadedINI);

            Lighting.WriteToIniFile(LoadedINI);

            MapWriter.WriteHouses(this, LoadedINI);

            MapWriter.WriteSmudges(this, LoadedINI);
            MapWriter.WriteOverlays(this, LoadedINI);
            MapWriter.WriteTerrainObjects(this, LoadedINI);
            MapWriter.WriteTubes(this, LoadedINI);

            MapWriter.WriteWaypoints(this, LoadedINI);
            MapWriter.WriteTaskForces(this, LoadedINI);
            MapWriter.WriteTriggers(this, LoadedINI);
            MapWriter.WriteTags(this, LoadedINI);
            MapWriter.WriteCellTags(this, LoadedINI);
            MapWriter.WriteScripts(this, LoadedINI);
            MapWriter.WriteTeamTypes(this, LoadedINI);
            MapWriter.WriteLocalVariables(this, LoadedINI);

            MapWriter.WriteAircraft(this, LoadedINI);
            MapWriter.WriteUnits(this, LoadedINI);
            MapWriter.WriteInfantry(this, LoadedINI);
            MapWriter.WriteBuildings(this, LoadedINI);

            //LoadedINI.WriteIniFile(LoadedINI.FileName.Substring(0, LoadedINI.FileName.Length - 4) + "_test.map");
            LoadedINI.WriteIniFile(LoadedINI.FileName);

            MapWritten?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Finds a house with the given name from the map's or the game's house lists.
        /// If no house is found, creates one and adds it to the game's house list.
        /// Returns the house that was found or created.
        /// </summary>
        /// <param name="houseName">The name of the house to find.</param>
        public House FindOrMakeHouse(string houseName)
        {
            var house = Houses.Find(h => h.ININame == houseName);
            if (house != null)
                return house;

            house = StandardHouses.Find(h => h.ININame == houseName);
            if (house != null)
                return house;

            house = new House(houseName);
            StandardHouses.Add(house);
            return house;
        }

        /// <summary>
        /// Finds a house with the given name from the map's or the game's house lists.
        /// Returns null if no house is found.
        /// </summary>
        /// <param name="houseName">The name of the house to find.</param>
        public House FindHouse(string houseName)
        {
            var house = Houses.Find(h => h.ININame == houseName);
            if (house != null)
                return house;

            return StandardHouses.Find(h => h.ININame == houseName);
        }

        private bool IsCoordWithinMap(Point2D coord)
        {
            if (coord.X <= 0 || coord.Y <= 0)
                return false;

            // Filter out cells that would be above (to the north of) the map area
            if (coord.X + coord.Y < Size.X + 1)
                return false;

            // Filter out cells that would be to the right (east) of the map area
            if (coord.X - coord.Y > Size.X - 1)
                return false;

            // Filter out cells that would be to the left (west) of the map area
            if (coord.Y - coord.X > Size.X - 1)
                return false;

            // Filter out cells that would be below (to the south of) the map area
            if (coord.Y + coord.X > Size.Y * 2 + Size.X)
                return false;

            return true;
        }

        public void SetTileData(List<MapTile> tiles)
        {
            if (tiles != null)
            {
                foreach (var tile in tiles)
                {
                    if (!IsCoordWithinMap(tile.CoordsToPoint()))
                    {
                        Logger.Log("Dropping cell " + tile.CoordsToPoint() + " that would be outside of the allowed map area.");
                        continue;
                    }

                    Tiles[tile.Y][tile.X] = tile;
                }
            }

            // Check for uninitialized tiles within the map bounds
            // Begin from the top-left corner and proceed row by row
            int ox = 1;
            int oy = Size.X;
            while (ox <= Size.Y)
            {
                int tx = ox;
                int ty = oy;
                while (tx < Size.X + ox)
                {
                    if (Tiles[ty][tx] == null)
                    {
                        Tiles[ty][tx] = new MapTile() { X = (short)tx, Y = (short)ty };
                    }

                    if (tx < Size.X + ox - 1 && Tiles[ty][tx + 1] == null)
                    {
                        Tiles[ty][tx + 1] = new MapTile() { X = (short)(tx + 1), Y = (short)ty };
                    }

                    tx++;
                    ty--;
                }

                ox++;
                oy++;
            }
        }

        public int GetCellCount()
        {
            int cellCount = 0;

            int ox = 1;
            int oy = Size.X;
            while (ox <= Size.Y)
            {
                int tx = ox;
                int ty = oy;
                while (tx < Size.X + ox)
                {
                    cellCount += 2;

                    tx++;
                    ty--;
                }

                ox++;
                oy++;
            }

            return cellCount;
        }

        /// <summary>
        /// Resizes the map. Handles moving map elements (objects, waypoints etc.) as required.
        /// Deletes elements that would end up outside of the new map borders.
        /// </summary>
        /// <param name="newSize">The new size of the map.</param>
        /// <param name="eastShift">Defines how many coords existing cells and 
        /// objects should be moved to the east.</param>
        /// <param name="southShift">Defines by how many coords existing cells and 
        /// objects should be moved to the south.</param>
        public void Resize(Point2D newSize, int eastShift, int southShift)
        {
            // Copy current cell list to preserve it
            MapTile[][] cells = Tiles;

            // Combine all cells into one single-dimensional list
            List<MapTile> allCellsInList = cells.Aggregate(new List<MapTile>(), (totalCellList, rowCellList) =>
            {
                var nonNullValues = rowCellList.Where(mapcell => mapcell != null);
                return totalCellList.Concat(nonNullValues).ToList();
            });

            // Handle east-shift
            // We can shift a cell one point towards the east by 
            // adding 1 to its X coordinate and subtracting 1 from its Y coordinate
            allCellsInList.ForEach(mapCell => { mapCell.ShiftPosition(eastShift, -eastShift); });

            // Handle south-shift
            // We can shift a cell one point towards the south by 
            // adding 1 to both its X and Y coordinates
            allCellsInList.ForEach(mapCell => { mapCell.ShiftPosition(southShift, southShift); });


            // Then the "fun" part. Shift every object, waypoint, celltag etc. similarly!
            ShiftObjectsInList(Aircraft, eastShift, southShift);
            ShiftObjectsInList(Infantry, eastShift, southShift);
            ShiftObjectsInList(Units, eastShift, southShift);
            ShiftObjectsInList(Structures, eastShift, southShift);
            ShiftObjectsInList(TerrainObjects, eastShift, southShift);
            ShiftObjectsInList(Waypoints, eastShift, southShift);
            ShiftObjectsInList(CellTags, eastShift, southShift);

            // Tubes are slightly more complicated...
            Tubes.ForEach(tube =>
            {
                tube.ShiftPosition(eastShift, -eastShift);
                tube.ShiftPosition(southShift, southShift);
            });


            // Now let's apply our changes and remove stuff that would end up outside of the map

            Size = newSize;

            // Re-init cell list
            // This will automatically get rid of cells that would end up outside of the map
            InitCells();
            SetTileData(allCellsInList);

            // Objects we have to check manually
            // Luckily functional programming and our design makes this relatively painless!
            Aircraft = Aircraft.Where(a => IsCoordWithinMap(a.Position)).ToList();
            Infantry = Infantry.Where(i => IsCoordWithinMap(i.Position)).ToList();
            Units = Units.Where(u => IsCoordWithinMap(u.Position)).ToList();
            Structures = Structures.Where(s => IsCoordWithinMap(s.Position)).ToList();
            TerrainObjects = TerrainObjects.Where(t => IsCoordWithinMap(t.Position)).ToList();
            Waypoints = Waypoints.Where(wp => IsCoordWithinMap(wp.Position)).ToList();
            CellTags = CellTags.Where(ct => IsCoordWithinMap(ct.Position)).ToList();
            Tubes = Tubes.Where(tube => IsCoordWithinMap(tube.EntryPoint) && IsCoordWithinMap(tube.ExitPoint)).ToList();

            // We're done!
            MapResized?.Invoke(this, EventArgs.Empty);
        }

        private void ShiftObjectsInList<T>(List<T> list, int eastShift, int southShift) where T : IMovable
        {
            list.ForEach(element => ShiftObject(element, eastShift, southShift));
        }

        private void ShiftObject(IMovable movableObject, int eastShift, int southShift)
        {
            int x = movableObject.Position.X;
            int y = movableObject.Position.Y;

            x += eastShift;
            y -= eastShift;

            x += southShift;
            y += southShift;

            movableObject.Position = new Point2D(x, y);
        }

        private void InitCells()
        {
            Tiles = new MapTile[TileBufferSize][];
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tiles[i] = new MapTile[TileBufferSize];
            }
        }

        public void PlaceTerrainTileAt(ITileImage tile, Point2D cellCoords)
        {
            for (int i = 0; i < tile.SubTileCount; i++)
            {
                var subTile = tile.GetSubTile(i);
                if (subTile.TmpImage == null)
                    continue;

                Point2D offset = tile.GetSubTileCoordOffset(i).Value;

                var mapTile = GetTile(cellCoords + offset);
                if (mapTile == null)
                    continue;

                mapTile.TileImage = null;
                mapTile.TileIndex = tile.TileID;
                mapTile.SubTileIndex = (byte)i;
            }
        }

        public void AddWaypoint(Waypoint waypoint)
        {
            Waypoints.Add(waypoint);
            var cell = GetTile(waypoint.Position.X, waypoint.Position.Y);
            if (cell.Waypoint != null)
            {
                throw new InvalidOperationException($"Cell at {cell.CoordsToPoint()} already has a waypoint, skipping adding waypoint {waypoint.Identifier}");
            }

            cell.Waypoint = waypoint;
        }

        public void RemoveWaypoint(Waypoint waypoint)
        {
            var tile = GetTile(waypoint.Position);
            if (tile.Waypoint == waypoint)
            {
                Waypoints.Remove(waypoint);
                tile.Waypoint = null;
            }
        }

        public void RemoveWaypointFrom(Point2D cellCoords)
        {
            var tile = GetTile(cellCoords);
            if (tile.Waypoint != null)
            {
                Waypoints.Remove(tile.Waypoint);
                tile.Waypoint = null;
            }
        }

        public void AddTaskForce(TaskForce taskForce)
        {
            TaskForces.Add(taskForce);
        }

        public void AddTrigger(Trigger trigger)
        {
            Triggers.Add(trigger);
        }

        public void AddTag(Tag tag)
        {
            Tags.Add(tag);
        }

        public void AddCellTag(CellTag cellTag)
        {
            var tile = GetTile(cellTag.Position);
            if (tile.CellTag != null)
            {
                Logger.Log("Tile already has a celltag, skipping placing of celltag at " + cellTag.Position);
                return;
            }

            CellTags.Add(cellTag);
            tile.CellTag = cellTag;
        }

        public void RemoveCellTagFrom(Point2D cellCoords)
        {
            var tile = GetTile(cellCoords);
            if (tile.CellTag != null)
            {
                CellTags.Remove(tile.CellTag);
                tile.CellTag = null;
            }
        }

        public void AddScript(Script script)
        {
            Scripts.Add(script);
        }

        public void AddTeamType(TeamType teamType)
        {
            TeamTypes.Add(teamType);
        }

        public void AddHouses(List<House> houses)
        {
            if (houses.Count > 0)
            {
                Houses.AddRange(houses);
                HousesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void AddHouse(House house)
        {
            Houses.Add(house);
            HousesChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool DeleteHouse(House house)
        {
            if (Houses.Remove(house))
            {
                HousesChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        public void RegisterBaseNode(House house, BaseNode baseNode)
        {
            var buildingType = Rules.BuildingTypes.Find(bt => bt.ININame == baseNode.StructureTypeName) ??
                throw new KeyNotFoundException("Building type not found while adding base node: " + baseNode.StructureTypeName);

            GraphicalBaseNodes.Add(new GraphicalBaseNode(baseNode, buildingType, house));
        }

        public void UnregisterBaseNode(BaseNode baseNode)
        {
            int index = GraphicalBaseNodes.FindIndex(gbn => gbn.BaseNode == baseNode);
            if (index > -1)
                GraphicalBaseNodes.RemoveAt(index);
        }

        public void HouseColorUpdated(House house)
        {
            HouseColorChanged?.Invoke(this, new HouseEventArgs(house));
        }

        public void PlaceBuilding(Structure structure)
        {
            structure.ObjectType.ArtConfig.DoForFoundationCoords(offset =>
            {
                var cell = GetTile(structure.Position + offset);
                if (cell == null)
                    return;

                if (cell.Structure != null)
                    throw new InvalidOperationException("Cannot place a structure on a cell that already has a structure!");

                cell.Structure = structure;
            });

            if (structure.ObjectType.ArtConfig.FoundationX == 0 && structure.ObjectType.ArtConfig.FoundationY == 0)
            {
                GetTile(structure.Position).Structure = structure;
            }
            
            Structures.Add(structure);
        }

        public void RemoveBuilding(Point2D cellCoords)
        {
            var cell = GetTile(cellCoords);

            if (cell.Structure != null)
                RemoveBuilding(cell.Structure);
        }

        public void RemoveBuilding(Structure structure)
        {
            structure.ObjectType.ArtConfig.DoForFoundationCoords(offset =>
            {
                var cell = GetTile(structure.Position + offset);
                if (cell == null)
                    return;

                if (cell.Structure == structure)
                    cell.Structure = null;
            });

            if (structure.ObjectType.ArtConfig.FoundationX == 0 && structure.ObjectType.ArtConfig.FoundationY == 0)
            {
                GetTile(structure.Position).Structure = null;
            }

            Structures.Remove(structure);
        }

        public void MoveBuilding(Structure structure, Point2D newCoords)
        {
            RemoveBuilding(structure);
            structure.Position = newCoords;
            PlaceBuilding(structure);
        }

        public void PlaceUnit(Unit unit)
        {
            var cell = GetTile(unit.Position);
            if (cell.Vehicle != null)
                throw new InvalidOperationException("Cannot place a vehicle on a cell that already has a vehicle!");

            cell.Vehicle = unit;
            Units.Add(unit);
        }

        public void RemoveUnit(Unit unit)
        {
            RemoveUnit(unit.Position);
        }

        public void RemoveUnit(Point2D cellCoords)
        {
            var cell = GetTile(cellCoords);
            Units.Remove(cell.Vehicle);
            cell.Vehicle = null;
        }

        public void MoveUnit(Unit unit, Point2D newCoords)
        {
            RemoveUnit(unit);
            unit.Position = newCoords;
            PlaceUnit(unit);
        }

        public void PlaceInfantry(Infantry infantry)
        {
            var cell = GetTile(infantry.Position);
            if (cell.Infantry[(int)infantry.SubCell] != null)
                throw new InvalidOperationException("Cannot place infantry on an occupied sub-cell spot!");

            cell.Infantry[(int)infantry.SubCell] = infantry;
            Infantry.Add(infantry);
        }

        public void RemoveInfantry(Infantry infantry)
        {
            var cell = GetTile(infantry.Position);
            cell.Infantry[(int)infantry.SubCell] = null;
            Infantry.Remove(infantry);
        }

        public void MoveInfantry(Infantry infantry, Point2D newCoords)
        {
            var newCell = GetTile(newCoords);
            SubCell freeSubCell = newCell.GetFreeSubCellSpot();
            RemoveInfantry(infantry);
            infantry.Position = newCoords;
            infantry.SubCell = freeSubCell;
            PlaceInfantry(infantry);
        }

        public void PlaceAircraft(Aircraft aircraft)
        {
            var cell = GetTile(aircraft.Position);
            if (cell.Aircraft != null)
                throw new InvalidOperationException("Cannot place an aircraft on a cell that already has an aircraft!");

            cell.Aircraft = aircraft;
            Aircraft.Add(aircraft);
        }

        public void RemoveAircraft(Aircraft aircraft)
        {
            var cell = GetTile(aircraft.Position);
            cell.Aircraft = null;
            Aircraft.Remove(aircraft);
        }

        public void MoveAircraft(Aircraft aircraft, Point2D newCoords)
        {
            RemoveAircraft(aircraft);
            aircraft.Position = newCoords;
            PlaceAircraft(aircraft);
        }

        public void AddTerrainObject(TerrainObject terrainObject)
        {
            var cell = GetTile(terrainObject.Position);
            if (cell.TerrainObject != null)
                throw new InvalidOperationException("Cannot place a terrain object on a cell that already has a terrain object!");

            cell.TerrainObject = terrainObject;
            TerrainObjects.Add(terrainObject);
        }

        public void RemoveTerrainObject(TerrainObject terrainObject)
        {
            RemoveTerrainObject(terrainObject.Position);
        }

        public void RemoveTerrainObject(Point2D cellCoords)
        {
            var cell = GetTile(cellCoords);
            TerrainObjects.Remove(cell.TerrainObject);
            cell.TerrainObject = null;
        }

        public void MoveTerrainObject(TerrainObject terrainObject, Point2D newCoords)
        {
            RemoveTerrainObject(terrainObject.Position);
            terrainObject.Position = newCoords;
            AddTerrainObject(terrainObject);
        }

        public void MoveWaypoint(Waypoint waypoint, Point2D newCoords)
        {
            RemoveWaypoint(waypoint);
            waypoint.Position = newCoords;
            AddWaypoint(waypoint);
        }

        /// <summary>
        /// Determines whether an object can be moved to a specific location.
        /// </summary>
        /// <param name="gameObject">The object to move.</param>
        /// <param name="newCoords">The new coordinates of the object.</param>
        /// <returns>True if the object can be moved, otherwise false.</returns>
        public bool CanMoveObject(IMovable movable, Point2D newCoords)
        {
            if (movable.WhatAmI() == RTTIType.Building)
            {
                bool canPlace = true;

                ((Structure)movable).ObjectType.ArtConfig.DoForFoundationCoords(offset =>
                {
                    MapTile foundationCell = GetTile(newCoords + offset);
                    if (foundationCell == null)
                        return;

                    if (foundationCell.Structure != null && foundationCell.Structure != movable)
                        canPlace = false;
                });

                if (!canPlace)
                    return false;
            }

            MapTile cell = GetTile(newCoords);
            if (movable.WhatAmI() == RTTIType.Waypoint)
                return cell.Waypoint == null;

            return cell.CanAddObject((GameObject)movable);
        }

        public void DeleteObjectFromCell(Point2D cellCoords)
        {
            var tile = GetTile(cellCoords.X, cellCoords.Y);
            if (tile == null)
                return;

            for (int i = 0; i < tile.Infantry.Length; i++)
            {
                if (tile.Infantry[i] != null)
                {
                    RemoveInfantry(tile.Infantry[i]);
                    return;
                }
            }

            if (tile.Aircraft != null)
            {
                RemoveAircraft(tile.Aircraft);
                return;
            }

            if (tile.Vehicle != null)
            {
                RemoveUnit(tile.Vehicle);
                return;
            }

            if (tile.Structure != null)
            {
                RemoveBuilding(tile.Structure);
                return;
            }

            if (tile.TerrainObject != null)
            {
                RemoveTerrainObject(tile.CoordsToPoint());
                return;
            }

            if (tile.CellTag != null)
            {
                RemoveCellTagFrom(tile.CoordsToPoint());
                return;
            }

            if (tile.Waypoint != null)
            {
                RemoveWaypoint(tile.Waypoint);
                return;
            }
        }

        public int GetOverlayFrameIndex(Point2D cellCoords)
        {
            var cell = GetTile(cellCoords);
            if (cell.Overlay == null || cell.Overlay.OverlayType == null)
                return Constants.NO_OVERLAY;

            if (!cell.Overlay.OverlayType.Tiberium)
                return cell.Overlay.FrameIndex;

            // Smooth out tiberium

            int[] frameIndexesForEachAdjacentTiberiumCell = { 0, 1, 3, 4, 6, 7, 8, 10, 11 };
            int adjTiberiumCount = 0;

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (y == 0 && x == 0)
                        continue;

                    var otherTile = GetTile(cellCoords + new Point2D(x, y));
                    if (otherTile != null && otherTile.Overlay != null)
                    {
                        if (otherTile.Overlay.OverlayType.Tiberium)
                            adjTiberiumCount++;
                    }
                }
            }

            return frameIndexesForEachAdjacentTiberiumCell[adjTiberiumCount];
        }

        public void DoForAllValidTiles(Action<MapTile> action)
        {
            for (int y = 0; y < Tiles.Length; y++)
            {
                for (int x = 0; x < Tiles[y].Length; x++)
                {
                    MapTile tile = Tiles[y][x];

                    if (tile == null)
                        continue;

                    action(tile);
                }
            }
        }

        public void SortWaypoints() => Waypoints = Waypoints.OrderBy(wp => wp.Identifier).ToList();

        public int GetAutoLATIndex(MapTile mapTile, int baseLATTileSetIndex, int transitionLATTileSetIndex, Func<TileSet, bool> miscChecker)
        {
            foreach (var autoLatData in AutoLATType.AutoLATData)
            {
                if (TransitionArrayDataMatches(autoLatData.TransitionMatchArray, mapTile, baseLATTileSetIndex, transitionLATTileSetIndex, miscChecker))
                {
                    return autoLatData.TransitionTypeIndex;
                }
            }

            return -1;
        }



        /// <summary>
        /// Convenience structure for <see cref="TransitionArrayDataMatches(int[], MapTile, int, int)"/>.
        /// </summary>
        struct NearbyTileData
        {
            public int XOffset;
            public int YOffset;
            public int DirectionIndex;

            public NearbyTileData(int xOffset, int yOffset, int directionIndex)
            {
                XOffset = xOffset;
                YOffset = yOffset;
                DirectionIndex = directionIndex;
            }
        }

        /// <summary>
        /// Checks if specific transition data matches for a tile.
        /// If it does, then the tile should use the LAT transition index related to the data.
        /// </summary>
        private bool TransitionArrayDataMatches(int[] transitionData, MapTile mapTile, int desiredTileSetId1, int desiredTileSetId2, Func<TileSet, bool> miscChecker)
        {
            var nearbyTiles = new NearbyTileData[]
            {
                new NearbyTileData(0, -1, AutoLATType.NE_INDEX),
                new NearbyTileData(-1, 0, AutoLATType.NW_INDEX),
                new NearbyTileData(0, 0, AutoLATType.CENTER_INDEX),
                new NearbyTileData(1, 0, AutoLATType.SE_INDEX),
                new NearbyTileData(0, 1, AutoLATType.SW_INDEX)
            };

            foreach (var nearbyTile in nearbyTiles)
            {
                if (!TileSetMatchesExpected(mapTile.X + nearbyTile.XOffset, mapTile.Y + nearbyTile.YOffset,
                    transitionData, nearbyTile.DirectionIndex, desiredTileSetId1, desiredTileSetId2, miscChecker))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TileSetMatchesExpected(int x, int y, int[] transitionData, int transitionDataIndex, int desiredTileSetId1, int desiredTileSetId2, Func<TileSet, bool> miscChecker)
        {
            var tile = GetTile(x, y);

            if (tile == null)
                return true;

            bool shouldMatch = transitionData[transitionDataIndex] > 0;

            int tileSetId = TheaterInstance.GetTileSetId(tile.TileIndex);
            var tileSet = TheaterInstance.Theater.TileSets[tileSetId];
            if (shouldMatch && (tileSetId != desiredTileSetId1 && tileSetId != desiredTileSetId2 && (miscChecker == null || !miscChecker(tileSet))))
                return false;

            if (!shouldMatch && (tileSetId == desiredTileSetId1 || tileSetId == desiredTileSetId2 || (miscChecker != null && miscChecker(tileSet))))
                return false;

            return true;
        }

        /// <summary>
        /// Generates an unique internal ID.
        /// Used for new TaskForces, Scripts, TeamTypes and Triggers.
        /// </summary>
        /// <returns></returns>
        public string GetNewUniqueInternalId()
        {
            int id = 1000000;
            string idString = string.Empty;

            while (true)
            {
                idString = "0" + id.ToString(CultureInfo.InvariantCulture);

                if (TaskForces.Exists(tf => tf.ININame == idString) || 
                    Scripts.Exists(s => s.ININame == idString) || 
                    TeamTypes.Exists(tt => tt.ININame == idString) ||
                    Triggers.Exists(t => t.ID == idString) || Tags.Exists(t => t.ID == idString) ||
                    LoadedINI.SectionExists(idString))
                {
                    id++;
                    continue;
                }

                break;
            }

            return idString;
        }

        // public void StartNew(IniFile rulesIni, IniFile firestormIni, TheaterType theaterType, Point2D size)
        // {
        //     Initialize(rulesIni, firestormIni);
        //     LoadedINI = new IniFile();
        // }

        public void Initialize(IniFile rulesIni, IniFile firestormIni, IniFile artIni, IniFile artFirestormIni)
        {
            if (rulesIni == null)
                throw new ArgumentNullException(nameof(rulesIni));

            Rules = new Rules();
            Rules.InitFromINI(rulesIni, initializer);

            var editorRulesIni = new IniFile(Environment.CurrentDirectory + "/Config/EditorRules.ini");

            StandardHouses = Rules.GetStandardHouses(editorRulesIni);
            if (StandardHouses.Count == 0)
                StandardHouses = Rules.GetStandardHouses(rulesIni);

            if (firestormIni != null)
            {
                Rules.InitFromINI(firestormIni, initializer);
            }

            Rules.InitArt(artIni, initializer);

            if (artFirestormIni != null)
            {
                Rules.InitArt(artFirestormIni, initializer);
            }

            // Load impassable cell information for terrain types
            var impassableTerrainObjectsIni = new IniFile(Environment.CurrentDirectory + "/Config/TerrainTypeImpassability.ini");

            Rules.TerrainTypes.ForEach(tt =>
            {
                string value = impassableTerrainObjectsIni.GetStringValue(tt.ININame, "ImpassableCells", null);
                if (string.IsNullOrWhiteSpace(value))
                    return;

                string[] cellInfos = value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var cellInfo in cellInfos)
                {
                    Point2D point = Point2D.FromString(cellInfo);
                    if (tt.ImpassableCells == null)
                        tt.ImpassableCells = new List<Point2D>(2);

                    tt.ImpassableCells.Add(point);
                }
            });
        }

        /// <summary>
        /// Checks the map for issues.
        /// Returns a list of issues found.
        /// </summary>
        public List<string> CheckForIssues()
        {
            var issueList = new List<string>();

            DoForAllValidTiles(cell =>
            {
                // Check whether the cell has tiberium on an impassable terrain type
                if (cell.HasTiberium())
                {
                    ITileImage tile = TheaterInstance.GetTile(cell.TileIndex);
                    ISubTileImage subTile = tile.GetSubTile(cell.SubTileIndex);

                    if (Helpers.IsLandTypeImpassable(subTile.TmpImage.TerrainType, true))
                    {
                        issueList.Add($"Cell at {cell.CoordsToPoint()} has tiberium on an otherwise impassable cell. This can cause harvesters to get stuck.");
                    }
                }
            });

            // Check for teamtypes having no taskforce or script
            TeamTypes.ForEach(tt =>
            {
                if (tt.TaskForce == null)
                    issueList.Add($"TeamType \"{tt.Name}\" has no TaskForce set!");

                if (tt.Script == null)
                    issueList.Add($"TeamType \"{tt.Name}\" has no Script set!");
            });

            const int EnableTriggerActionIndex = 53;
            const int EnableTriggerParamIndex = 1;

            // Check for triggers that are disabled and are never enabled by any other triggers
            Triggers.ForEach(trigger =>
            {
                if (!trigger.Disabled)
                    return;

                const int RevealAllMapActionIndex = 16;

                // If this trigger has a "reveal all map" action, don't create an issue - those are usually only for debugging
                if (trigger.Actions.Exists(a => a.ActionIndex == RevealAllMapActionIndex))
                    return;

                // Allow the user to skip this warning by including "DEBUG" in the trigger's name
                if (trigger.Name.ToUpperInvariant().Contains("DEBUG"))
                    return;

                // Is this trigger enabled by another trigger?
                if (Triggers.Exists(otherTrigger => otherTrigger != trigger && otherTrigger.Actions.Exists(a => a.ActionIndex == EnableTriggerActionIndex && a.Parameters[EnableTriggerParamIndex] == trigger.ID)))
                    return;

                // If it's not enabled by another trigger, add an issue
                issueList.Add($"Trigger \"{trigger.Name}\" ({trigger.ID}) is disabled and never enabled by another trigger." + Environment.NewLine +
                    "Did you forget to enable it? If the trigger exists for debugging purposes, add DEBUG to its name to skip this warning.");
            });

            // Check for triggers that enable themselves, there's no need to ever do this -> either redundant action or a scripting error
            Triggers.ForEach(trigger =>
            {
                if (!trigger.Actions.Exists(a => a.ActionIndex == EnableTriggerActionIndex && a.Parameters[EnableTriggerParamIndex] == trigger.ID))
                    return;

                issueList.Add($"Trigger \"{trigger.Name}\" ({trigger.ID}) has an action for enabling itself. Is it supposed to enable something else instead?");
            });

            // Check that the primary player house has "Player Control" enabled in case [Basic] Player= is specified
            // (iow. this is a singleplayer mission)
            if (!string.IsNullOrWhiteSpace(Basic.Player) && !Helpers.IsStringNoneValue(Basic.Player))
            {
                House matchingHouse = GetHouses().Find(h => h.ININame == Basic.Player);
                if (matchingHouse == null)
                    issueList.Add("A nonexistent house has been specified in [Basic] Player= .");
                else if (!matchingHouse.PlayerControl)
                    issueList.Add("The human player's house does not have the \"Player-Controlled\" flag checked.");
            }

            return issueList;
        }
    }
}