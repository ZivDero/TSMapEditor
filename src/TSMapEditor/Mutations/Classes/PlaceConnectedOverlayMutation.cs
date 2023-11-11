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

            brush.DoForBrushSize(offset =>
            {
                var tile = MutationTarget.Map.GetTile(cellCoords + offset);
                if (tile == null)
                    return;

                for (int xOffset = -1; xOffset <= 1; xOffset++)
                {
                    for (int yOffset = -1; yOffset <= 1; yOffset++)
                    {
                        var originalTile = MutationTarget.Map.GetTile(cellCoords + new Point2D(xOffset, yOffset));
                        if (originalTile == null)
                            continue;

                        originalOverlayInfos.Add(new OriginalOverlayInfo()
                        {
                            CellCoords = originalTile.CoordsToPoint(),
                            OverlayTypeIndex = originalTile.Overlay?.OverlayType.Index ?? -1,
                            FrameIndex = originalTile.Overlay?.FrameIndex ?? -1,
                        });
                    }
                }

                if (connectedOverlayType != null)
                {
                    var connectedOverlayFrame = connectedOverlayType.GetOverlayForCell(MutationTarget, cellCoords + offset) ?? connectedOverlayType.Frames[0];

                    tile.Overlay = new Overlay()
                    {
                        Position = tile.CoordsToPoint(),
                        OverlayType = connectedOverlayFrame.OverlayType,
                        FrameIndex = connectedOverlayFrame.FrameIndex
                    };

                    UpdateNeighborTiles(cellCoords + offset);
                }
                else
                {
                    tile.Overlay = null;
                }
            });

            undoData = originalOverlayInfos.ToArray();
            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brush.Width, brush.Height) + 1);
        }

        private void UpdateNeighborTiles(Point2D cellCoords)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    if (xOffset == 0 && yOffset == 0)
                        continue;

                    var tile = MutationTarget.Map.GetTile(cellCoords + new Point2D(xOffset, yOffset));

                    if (tile?.Overlay == null || !connectedOverlayType.ContainsOverlay(tile.Overlay))
                        continue;

                    var connectedOverlayFrame = connectedOverlayType.GetOverlayForCell(MutationTarget, tile.CoordsToPoint());
                    if (connectedOverlayFrame == null)
                        continue;

                    tile.Overlay = new Overlay()
                    {
                        Position = tile.CoordsToPoint(),
                        OverlayType = connectedOverlayFrame.OverlayType,
                        FrameIndex = connectedOverlayFrame.FrameIndex
                    };
                }
            }
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
