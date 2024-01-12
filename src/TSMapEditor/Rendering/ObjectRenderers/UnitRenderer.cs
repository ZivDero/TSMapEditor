﻿using Microsoft.CodeAnalysis.Operations;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using SharpDX.Direct3D9;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class UnitRenderer : ObjectRenderer<Unit>
    {
        public UnitRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Red;

        protected override CommonDrawParams GetDrawParams(Unit gameObject)
        {
            if (gameObject.UnitType.ArtConfig.Voxel)
            {
                var graphics = TheaterGraphics.UnitModels[gameObject.ObjectType.Index];
                string iniName = gameObject.ObjectType.ININame;
                return new VoxelDrawParams(graphics, iniName);
            }
            else
            {
                var graphics = TheaterGraphics.UnitTextures[gameObject.ObjectType.Index];
                string iniName = gameObject.ObjectType.ININame;
                return new ShapeDrawParams(graphics, iniName);
            }
        }

        protected override void Render(Unit gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint,
            CommonDrawParams drawParams)
        {
            if (drawParams is ShapeDrawParams shapeDrawParams)
                RenderShape(gameObject, yDrawPointWithoutCellHeight, drawPoint, shapeDrawParams);
            else if (drawParams is VoxelDrawParams voxelDrawParams)
                RenderVoxelModel(gameObject, yDrawPointWithoutCellHeight, drawPoint, voxelDrawParams);
        }

        private void RenderShape(Unit gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint,
            ShapeDrawParams drawParams)
        {
            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject, drawParams, drawPoint, yDrawPointWithoutCellHeight);

            DrawObjectImage(gameObject, drawParams, drawParams.Graphics, 
                gameObject.GetFrameIndex(drawParams.Graphics.Frames.Length),
                Color.White, true, gameObject.GetRemapColor(), drawPoint, yDrawPointWithoutCellHeight);

            if (gameObject.UnitType.Turret)
            {
                int turretFrameIndex = gameObject.GetTurretFrameIndex();
                if (turretFrameIndex > -1 && turretFrameIndex < drawParams.Graphics.Frames.Length)
                {
                    PositionedTexture frame = drawParams.Graphics.Frames[turretFrameIndex];

                    if (frame == null)
                        return;

                    DrawObjectImage(gameObject, drawParams, drawParams.Graphics, 
                        turretFrameIndex, Color.White, true, gameObject.GetRemapColor(),
                        drawPoint, yDrawPointWithoutCellHeight);
                }
            }
        }

        private void RenderVoxelModel(Unit gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint,
            VoxelDrawParams drawParams)
        {
            var unitTile = RenderDependencies.Map.GetTile(gameObject.Position.X, gameObject.Position.Y);

            if (unitTile == null)
                return;

            ITileImage tile = RenderDependencies.Map.TheaterInstance.GetTile(unitTile.TileIndex);
            ISubTileImage subTile = tile.GetSubTile(unitTile.SubTileIndex);
            RampType ramp = subTile.TmpImage.RampType;

            DrawVoxelModel(gameObject, drawParams, drawParams.Graphics,
                gameObject.Facing, ramp, Color.White, true, gameObject.GetRemapColor(),
                drawPoint, yDrawPointWithoutCellHeight);
        }
    }
}
