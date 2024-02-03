using Microsoft.Xna.Framework;
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
            string iniName = gameObject.ObjectType.ININame;

            return new CommonDrawParams()
            {
                IniName = iniName,
                MainImage = TheaterGraphics.UnitTextures[gameObject.ObjectType.Index],
                MainModel = TheaterGraphics.UnitModels[gameObject.ObjectType.Index],
                TurretModel = TheaterGraphics.UnitTurretModels[gameObject.ObjectType.Index],
                BarrelModel = TheaterGraphics.UnitBarrelModels[gameObject.ObjectType.Index]
            };
        }

        protected override void Render(Unit gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            if (gameObject.UnitType.ArtConfig.Voxel)
            {
                RenderVoxelModel(gameObject, heightOffset, drawPoint, drawParams, drawParams.MainModel);
                
                const byte facingStartDrawAbove = (byte)Direction.NE * 32;
                const byte facingEndDrawAbove = (byte)Direction.SW * 32;

                if (gameObject.Facing is > facingStartDrawAbove and <= facingEndDrawAbove)
                {
                    RenderVoxelModel(gameObject, heightOffset, drawPoint, drawParams, drawParams.TurretModel);
                    RenderVoxelModel(gameObject, heightOffset, drawPoint, drawParams, drawParams.BarrelModel);
                }
                else
                {
                    RenderVoxelModel(gameObject, heightOffset, drawPoint, drawParams, drawParams.BarrelModel);
                    RenderVoxelModel(gameObject, heightOffset, drawPoint, drawParams, drawParams.TurretModel);
                }
            }
            else
            {
                RenderShape(gameObject, heightOffset, drawPoint, drawParams);
            }
        }

        private void RenderShape(Unit gameObject, int heightOffset, Point2D drawPoint,
            CommonDrawParams drawParams)
        {
            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject, drawParams, drawPoint, heightOffset);

            DrawShapeImage(gameObject, drawParams, drawParams.MainImage, 
                gameObject.GetFrameIndex(drawParams.MainImage.GetFrameCount()),
                Color.White, true, gameObject.GetRemapColor(), drawPoint, heightOffset);

            if (gameObject.UnitType.Turret)
            {
                int turretFrameIndex = gameObject.GetTurretFrameIndex();

                if (turretFrameIndex > -1 && turretFrameIndex < drawParams.MainImage.GetFrameCount())
                {
                    PositionedTexture frame = drawParams.MainImage.GetFrame(turretFrameIndex);

                    if (frame == null)
                        return;

                    DrawShapeImage(gameObject, drawParams, drawParams.MainImage, 
                        turretFrameIndex, Color.White, true, gameObject.GetRemapColor(),
                        drawPoint, heightOffset);
                }
            }
        }

        private void RenderVoxelModel(Unit gameObject, int heightOffset, Point2D drawPoint,
            in CommonDrawParams drawParams, VoxelModel model)
        {
            var unitTile = RenderDependencies.Map.GetTile(gameObject.Position.X, gameObject.Position.Y);

            if (unitTile == null)
                return;

            ITileImage tile = RenderDependencies.Map.TheaterInstance.GetTile(unitTile.TileIndex);
            ISubTileImage subTile = tile.GetSubTile(unitTile.SubTileIndex);
            RampType ramp = subTile.TmpImage.RampType;

            DrawVoxelModel(gameObject, drawParams, model,
                gameObject.Facing, ramp, Color.White, true, gameObject.GetRemapColor(),
                drawPoint, heightOffset);
        }
    }
}
