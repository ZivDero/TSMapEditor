using Microsoft.CodeAnalysis.Operations;
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
                return new CommonDrawParams(graphics, iniName);
            }
            else
            {
                var graphics = TheaterGraphics.UnitTextures[gameObject.ObjectType.Index];
                string iniName = gameObject.ObjectType.ININame;
                return new CommonDrawParams(graphics, iniName);
            }
        }

        protected override void Render(Unit gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint,
            CommonDrawParams commonDrawParams)
        {
            if (commonDrawParams.Graphics is ObjectImage)
                RenderShape(gameObject, yDrawPointWithoutCellHeight, drawPoint, commonDrawParams);
            else if (commonDrawParams.Graphics is VoxelModel)
                RenderVoxelModel(gameObject, yDrawPointWithoutCellHeight, drawPoint, commonDrawParams);
        }

        private void RenderShape(Unit gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint,
            CommonDrawParams commonDrawParams)
        {
            if (commonDrawParams.Graphics is not ObjectImage graphics)
                return;

            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject, commonDrawParams, drawPoint, yDrawPointWithoutCellHeight);

            DrawObjectImage(gameObject, commonDrawParams, graphics, 
                gameObject.GetFrameIndex(graphics.Frames.Length),
                Color.White, true, gameObject.GetRemapColor(), drawPoint, yDrawPointWithoutCellHeight);

            if (gameObject.UnitType.Turret)
            {
                int turretFrameIndex = gameObject.GetTurretFrameIndex();
                if (turretFrameIndex > -1 && turretFrameIndex < graphics.Frames.Length)
                {
                    PositionedTexture frame = graphics.Frames[turretFrameIndex];

                    if (frame == null)
                        return;

                    DrawObjectImage(gameObject, commonDrawParams, graphics, 
                        turretFrameIndex, Color.White, true, gameObject.GetRemapColor(),
                        drawPoint, yDrawPointWithoutCellHeight);
                }
            }
        }

        private void RenderVoxelModel(Unit gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint,
            CommonDrawParams commonDrawParams)
        {
            if (commonDrawParams.Graphics is not VoxelModel graphics)
                return;

            var unitTile = RenderDependencies.Map.GetTile(gameObject.Position.X, gameObject.Position.Y);

            if (unitTile == null)
                return;

            ITileImage tile = RenderDependencies.Map.TheaterInstance.GetTile(unitTile.TileIndex);
            ISubTileImage subTile = tile.GetSubTile(unitTile.SubTileIndex);
            RampType ramp = subTile.TmpImage.RampType;

            DrawVoxelModel(gameObject, commonDrawParams, graphics,
                gameObject.Facing, ramp, Color.White, true, gameObject.GetRemapColor(),
                drawPoint, yDrawPointWithoutCellHeight);
        }
    }
}
