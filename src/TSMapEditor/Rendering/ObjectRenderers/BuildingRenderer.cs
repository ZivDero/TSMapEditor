﻿using Microsoft.Xna.Framework;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class BuildingRenderer : ObjectRenderer<Structure>
    {
        public BuildingRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Yellow;

        private void DrawFoundationLines(Structure gameObject)
        {
            int foundationX = gameObject.ObjectType.ArtConfig.Foundation.Width;
            int foundationY = gameObject.ObjectType.ArtConfig.Foundation.Height;

            Color foundationLineColor = gameObject.Owner.XNAColor;

            if (foundationX == 0 || foundationY == 0)
                return;

            var map = RenderDependencies.Map;

            int heightOffset = 0;

            var cell = map.GetTile(gameObject.Position);
            if (cell != null && !RenderDependencies.EditorState.Is2DMode)
                heightOffset = cell.Level * Constants.CellHeight;

            SetEffectParams(0.0f, 0.0f, Vector2.Zero, Vector2.Zero);

            foreach (var edge in gameObject.ObjectType.ArtConfig.Foundation.Edges)
            {
                // Translate edge vertices from cell coordinate space to world coordinate space.
                var start = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position + edge[0], map);
                var end = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position + edge[1], map);
                // Height is an illusion, just move everything up or down.
                // Also offset X to match the top corner of an iso tile.
                start += new Point2D(Constants.CellSizeX / 2, -heightOffset);
                end += new Point2D(Constants.CellSizeX / 2, -heightOffset);
                // Draw edge.
                DrawLine(start.ToXNAVector(), end.ToXNAVector(), foundationLineColor, 1);
            }
        }

        protected override CommonDrawParams GetDrawParams(Structure gameObject)
        {
            string iniName = gameObject.ObjectType.ININame;

            return new CommonDrawParams()
            {
                IniName = iniName,
                MainImage = RenderDependencies.TheaterGraphics.BuildingTextures[gameObject.ObjectType.Index],
                TurretModel = RenderDependencies.TheaterGraphics.BuildingTurretModels[gameObject.ObjectType.Index],
                BarrelModel = RenderDependencies.TheaterGraphics.BuildingBarrelModels[gameObject.ObjectType.Index]
            };
        }

        protected override bool ShouldRenderReplacementText(Structure gameObject)
        {
            var bibGraphics = RenderDependencies.TheaterGraphics.BuildingBibTextures[gameObject.ObjectType.Index];

            if (bibGraphics != null)
                return false;

            if (gameObject.ObjectType.ArtConfig.TurretAnim != null)
                return false;

            return base.ShouldRenderReplacementText(gameObject);
        }

        private void DrawBibGraphics(Structure gameObject, ShapeImage bibGraphics, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            DrawShapeImage(gameObject, drawParams, bibGraphics, 0, Color.White, true, gameObject.GetRemapColor(), drawPoint, heightOffset);
        }

        protected override void Render(Structure gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            DrawFoundationLines(gameObject);

            var bibGraphics = RenderDependencies.TheaterGraphics.BuildingBibTextures[gameObject.ObjectType.Index];
            
            if (bibGraphics != null)
                DrawBibGraphics(gameObject, bibGraphics, heightOffset, drawPoint, drawParams);

            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject, drawParams, drawPoint, heightOffset);

            DrawShapeImage(gameObject, drawParams, drawParams.MainImage,
                gameObject.GetFrameIndex(drawParams.MainImage.GetFrameCount()),
                Color.White, true, gameObject.GetRemapColor(), drawPoint, heightOffset);

            if (gameObject.ObjectType.Turret && gameObject.ObjectType.TurretAnimIsVoxel)
            {
                var turretOffset = new Point2D(gameObject.ObjectType.TurretAnimX, gameObject.ObjectType.TurretAnimY);
                var turretDrawPoint = drawPoint + turretOffset;

                const byte facingStartDrawAbove = (byte)Direction.E * 32;
                const byte facingEndDrawAbove = (byte)Direction.W * 32;

                if (gameObject.Facing is > facingStartDrawAbove and <= facingEndDrawAbove)
                {
                    DrawVoxelModel(gameObject, drawParams, drawParams.TurretModel,
                        gameObject.Facing, RampType.None, Color.White, true, gameObject.GetRemapColor(),
                        turretDrawPoint, heightOffset);

                    DrawVoxelModel(gameObject, drawParams, drawParams.BarrelModel,
                        gameObject.Facing, RampType.None, Color.White, true, gameObject.GetRemapColor(),
                        turretDrawPoint, heightOffset);
                }
                else
                {
                    DrawVoxelModel(gameObject, drawParams, drawParams.BarrelModel,
                        gameObject.Facing, RampType.None, Color.White, true, gameObject.GetRemapColor(),
                        turretDrawPoint, heightOffset);

                    DrawVoxelModel(gameObject, drawParams, drawParams.TurretModel,
                        gameObject.Facing, RampType.None, Color.White, true, gameObject.GetRemapColor(),
                        turretDrawPoint, heightOffset);
                }
            }
            else if (gameObject.ObjectType.Turret && !gameObject.ObjectType.TurretAnimIsVoxel &&
                     gameObject.ObjectType.BarrelAnimIsVoxel)
            {
                DrawVoxelModel(gameObject, drawParams, drawParams.BarrelModel,
                    gameObject.Facing, RampType.None, Color.White, true, gameObject.GetRemapColor(),
                    drawPoint, heightOffset);
            }

            if (gameObject.ObjectType.HasSpotlight ||
                (gameObject.ObjectType.Turret && gameObject.ObjectType.TurretAnimIsVoxel && drawParams.TurretModel == null))
            {
                Point2D cellCenter = RenderDependencies.EditorState.Is2DMode ?
                    CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, Map) :
                    CellMath.CellTopLeftPointFromCellCoords_3D(gameObject.Position, Map);

                DrawObjectFacingArrow(gameObject.Facing, cellCenter);
            }
        }

        protected override void DrawObjectReplacementText(Structure gameObject, in CommonDrawParams drawParams, Point2D drawPoint)
        {
            DrawFoundationLines(gameObject);

            base.DrawObjectReplacementText(gameObject, drawParams, drawPoint);
        }
    }
}
