﻿using Rampastring.XNAUI;
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

        private const int MaxHScoreIterations = 500;

        public override void Perform()
        {
            for (int i = 0; i < cliffPath.Count - 1; i++)
            {
                DrawCliffAStar((Vector2)cliffPath[i], (Vector2)cliffPath[i + 1]);
            }

            MutationTarget.InvalidateMap();
        }

        private void DrawCliffAStar(Vector2 start, Vector2 end)
        {
            //HashSet<CliffAStarNode> closedSet = new HashSet<CliffAStarNode>();
            PriorityQueue<CliffAStarNode, float> openSet = new PriorityQueue<CliffAStarNode, float>();

            CliffAStarNode bestNode = null;
            float bestDistance = float.PositiveInfinity;

            int hScoreIterations = 0;

            var startNode = CliffAStarNode.MakeStartNode(start, end);

            openSet.Enqueue(startNode, startNode.FScore);

            // Temp until we properly handle changing the side
            var thisFace = cliffType.Tiles.Where(tile => tile.Side == currentSide).ToList();

            //float previousHScore = float.PositiveInfinity;

            while (openSet.Count > 0)
            {
                CliffAStarNode currentNode = openSet.Dequeue();
                openSet.EnqueueRange(currentNode.GetNeighbors(thisFace).Select(node => (node, node.FScore)));
                
                // keep track of how many times we've unsuccessfully tried to find a closer point
                if (currentNode.HScore < bestDistance)
                {
                    bestNode = currentNode;
                    bestDistance = currentNode.HScore;
                    hScoreIterations = 0;
                }
                else
                {
                    hScoreIterations++;
                }

                // terminate if we've been stuck for too long or we're at the destination
                if (hScoreIterations > MaxHScoreIterations || bestDistance < 1)
                    break;
            }

            PlaceAStarCliffs(bestNode);
        }

        private void PlaceAStarCliffs(CliffAStarNode endNode)
        {
            var node = endNode;
            while (node != null)
            {
                if (node.Tile != null)
                {
                    var tileImage = MutationTarget.TheaterGraphics.GetTileGraphics(tileSet.StartTileIndex + node.Tile.TileIndexInSet);
                    PlaceTile(tileImage, new Point2D((int)node.Location.X, (int)node.Location.Y));
                }

                node = node.Parent;
            }
        }

        private void PlaceTile(TileImage tile, Point2D targetCellCoords)
        {
            if (tile == null)
                return;

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
