using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing connected overlays.
    /// </summary>
    class PlaceConnectedOverlayMutation : Mutation
    {
        public PlaceConnectedOverlayMutation(IMutationTarget mutationTarget, ConnectedOverlayType connectedOverlayType, Point2D cellCoords) : base(mutationTarget)
        {
            this.connectedOverlayType = connectedOverlayType;
            this.cellCoords = cellCoords;
            brush = mutationTarget.BrushSize;
        }

        private readonly ConnectedOverlayType connectedOverlayType;
        private readonly BrushSize brush;
        private readonly Point2D cellCoords;

        private OriginalOverlayInfo[] undoData;

        public override void Perform()
        {
            var originalOverlayInfos = new List<OriginalOverlayInfo>();

            // Save the original overlays
            brush.DoForBrushSizeAndSurroundings(offset =>
            {
                var tile = MutationTarget.Map.GetTile(cellCoords + offset);
                if (tile == null)
                    return;

                originalOverlayInfos.Add(new OriginalOverlayInfo()
                {
                    CellCoords = tile.CoordsToPoint(),
                    OverlayTypeIndex = tile.Overlay?.OverlayType.Index ?? -1,
                    FrameIndex = tile.Overlay?.FrameIndex ?? -1,
                });
            });

            // Now place overlays and update all the tiles to make sure they are connected properly
            brush.DoForBrushSizeAndSurroundings(offset =>
            {
                var tile = MutationTarget.Map.GetTile(cellCoords + offset);
                if (tile == null)
                    return;

                bool isSurrounding = offset.X == -1 || offset.Y == -1 || offset.X == brush.Width || offset.Y == brush.Height;
                PlaceConnectedOverlay(tile, isSurrounding);
            });

            undoData = originalOverlayInfos.ToArray();
            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brush.Width, brush.Height) + 1);
        }

        private void PlaceConnectedOverlay(MapTile tile, bool updateOnly = false)
        {
            if (tile == null || (updateOnly && !connectedOverlayType.ContainsOverlay(tile.Overlay)))
                return;

            var connectedOverlayFrame = connectedOverlayType.GetOverlayForCell(MutationTarget, tile.CoordsToPoint());
            if (connectedOverlayFrame == null)
                return;

            tile.Overlay = new Overlay()
            {
                Position = tile.CoordsToPoint(),
                OverlayType = connectedOverlayFrame.OverlayType,
                FrameIndex = connectedOverlayFrame.FrameIndex
            };
        }

        public override void Undo()
        {
            foreach (OriginalOverlayInfo info in undoData)
            {
                var tile = MutationTarget.Map.GetTile(info.CellCoords);
                if (info.OverlayTypeIndex == -1)
                {
                    tile.Overlay = null;
                    continue;
                }

                tile.Overlay = new Overlay()
                {
                    OverlayType = MutationTarget.Map.Rules.OverlayTypes[info.OverlayTypeIndex],
                    Position = info.CellCoords,
                    FrameIndex = info.FrameIndex
                };
            }

            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brush.Width, brush.Height) + 1);
        }
    }
}
