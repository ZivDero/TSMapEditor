using Rampastring.XNAUI;
using SharpDX.Direct2D1.Effects;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation for drawing cliffs.
    /// </summary>
    public class DrawCliffMutation : Mutation
    {
        public DrawCliffMutation(IMutationTarget mutationTarget, List<Point2D> cliffPath, CliffType cliffType) : base(mutationTarget)
        {
            if (cliffPath.Count < 2)
            {
                throw new ArgumentException(nameof(DrawCliffMutation) +
                                            ": to draw a cliff at least 2 path vertices are required.");
            }

            this.cliffPath = cliffPath;
            this.cliffType = cliffType;
            this.originLevel = mutationTarget.Map.GetTile(cliffPath[0]).Level;
            this.tileSet = mutationTarget.Map.TheaterInstance.Theater.FindTileSet(cliffType.TileSet);
        }

        private readonly List<Point2D> cliffPath;

        private readonly CliffSide currentSide = CliffSide.Front;
        private readonly CliffType cliffType;
        private readonly int originLevel;
        private readonly TileSet tileSet;

        private CliffConnectionPoint lastConnectionPoint;
        private HashSet<Point2D> occupiedTiles;
        private Random random = new Random();

        public override void Perform()
        {
            lastConnectionPoint = new CliffConnectionPoint
            {
                ConnectsTo = 0b11111111,
                Coordinates = (Vector2)cliffPath[0]
            };

            for (int i = 0; i < cliffPath.Count - 1; i++)
            {
                DrawCliff((Vector2)cliffPath[i], (Vector2)cliffPath[i + 1]);
            }

            MutationTarget.InvalidateMap();
        }

        private void DrawCliff(Vector2 start, Vector2 end)
        {
            // Temp until we properly handle changing the side
            var thisFace = cliffType.Tiles.Where(tile => tile.Side == currentSide).ToList();
            float lastDistance = int.MaxValue;
            List<PotentialCliffPlacement> potentialPlacements = new List<PotentialCliffPlacement>();

            while (true)
            {
                potentialPlacements.Clear();

                foreach (var tile in thisFace)
                {
                    potentialPlacements.AddRange(lastConnectionPoint.ConnectTo(tile));
                }

                float minDistance = potentialPlacements
                    .Select(placement => (end - placement.NextConnectionPoint.Coordinates).Length()).Min();
                var bestPlacements = potentialPlacements.Where(placement => Math.Abs((end - placement.NextConnectionPoint.Coordinates).Length() - minDistance) < 0.01).ToList();
                var bestPlacement = bestPlacements.ElementAt(random.Next(0, bestPlacements.Count - 1));

                if (minDistance < lastDistance)
                {
                    lastDistance = minDistance;
                    lastConnectionPoint = bestPlacement.NextConnectionPoint;
                }
                else
                {
                    break;
                }

                var tileImage = MutationTarget.TheaterGraphics.GetTileGraphics(tileSet.StartTileIndex + bestPlacement.Tile.TileIndexInSet);
                PlaceTile(tileImage, new Point2D((int)bestPlacement.PlacementCoords.X, (int)bestPlacement.PlacementCoords.Y));
            }
        }

        private void PlaceTile(TileImage tile, Point2D targetCellCoords)
        {
            for (int i = 0; i < tile.TMPImages.Length; i++)
            {
                MGTMPImage image = tile.TMPImages[i];
                if (image.TmpImage == null)
                    continue;
                int cx = targetCellCoords.X + i % tile.Width;
                int cy = targetCellCoords.Y + i / tile.Width;

                var mapTile = MutationTarget.Map.GetTile(cx, cy);
                if (mapTile != null && (!MutationTarget.OnlyPaintOnClearGround || mapTile.IsClearGround()))
                {
                    mapTile.ChangeTileIndex(tile.TileID, (byte)i);
                    mapTile.Level = (byte)Math.Min(originLevel + image.TmpImage.Height, Constants.MaxMapHeightLevel);
                }
            }
        }

        public override void Undo()
        {
            MutationTarget.InvalidateMap();
        }

        
    }
}
