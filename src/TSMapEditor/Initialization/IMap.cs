using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;

namespace TSMapEditor.Initialization
{
    public interface IMap
    {
        BasicSection Basic { get; }

        bool TsCoop { get; set; }

        MapTile[][] Tiles { get; }
        MapTile GetTile(int x, int y);
        MapTile GetTile(Point2D cellCoords);
        SortedSet<HouseType> StandardHouseTypes { get; }
        List<House> StandardHouses { get; }
        SortedSet<HouseType> SpawnHouseTypes { get; set; }
        List<House> SpawnHouses { get; set; }
        SortedSet<HouseType> HouseTypes { get; }
        List<House> Houses { get; }
        List<Aircraft> Aircraft { get; }
        List<Infantry> Infantry { get; }
        IniFile LoadedINI { get; }
        Rules Rules { get; }
        EditorConfig EditorConfig { get; }
        List<Structure> Structures { get; }
        List<TerrainObject> TerrainObjects { get; }
        List<Unit> Units { get; }

        List<Waypoint> Waypoints { get; }
        List<Trigger> Triggers { get; }
        List<Tag> Tags { get; }
        List<CellTag> CellTags { get; }
        List<Script> Scripts { get; }
        List<TaskForce> TaskForces { get; }
        List<TeamType> TeamTypes { get; }
        List<AITriggerType> AITriggerTypes { get; }
        List<LocalVariable> LocalVariables { get; }
        List<Tube> Tubes { get; }

        Point2D Size { get; set; }
        Rectangle LocalSize { get; set; }
        string TheaterName { get; set; }

        void SetTileData(List<MapTile> tiles);
        void PlaceTerrainTileAt(ITileImage tile, Point2D cellCoords);
        House FindHouse(string houseName);
        HouseType FindHouseType(string houseTypeName);
        bool IsCoordWithinMap(int x, int y);
        bool IsCoordWithinMap(Point2D coord);
        void AddWaypoint(Waypoint waypoint);
        void AddTaskForce(TaskForce taskForce);
        void AddTrigger(Trigger trigger);
        void AddTag(Tag tag);
        void AddCellTag(CellTag cellTag);
        void RemoveCellTagFrom(Point2D cellCoords);
        void AddScript(Script script);
        void AddTeamType(TeamType teamType);

        void PlaceUnit(Unit unit);
        void RemoveUnit(Unit unit);

        void DoForAllValidTiles(Action<MapTile> action);

        void SortWaypoints();

        void Save();
        void AutoSave(string filePath);
    }
}