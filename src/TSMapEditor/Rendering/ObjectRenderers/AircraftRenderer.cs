using Microsoft.Xna.Framework;
using System;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    internal class AircraftRenderer : ObjectRenderer<Aircraft>
    {
        public AircraftRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.HotPink;

        protected override CommonDrawParams GetDrawParams(Aircraft gameObject)
        {
            if (gameObject.AircraftType.ArtConfig.Voxel)
            {
                var graphics = TheaterGraphics.AircraftModels[gameObject.ObjectType.Index];
                string iniName = gameObject.ObjectType.ININame;
                return new VoxelDrawParams(graphics, iniName);
            }
            else
            {
                return new ShapeDrawParams(null, gameObject.ObjectType.ININame);
            }
        }

        protected override void Render(Aircraft gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint, CommonDrawParams drawParams)
        {
            if (drawParams is VoxelDrawParams voxelDrawParams)
                RenderVoxelModel(gameObject, yDrawPointWithoutCellHeight, drawPoint, voxelDrawParams);
            else
                throw new NotImplementedException();
        }

        private void RenderVoxelModel(Aircraft gameObject, int yDrawPointWithoutCellHeight, Point2D drawPoint,
            VoxelDrawParams drawParams)
        {
            DrawVoxelModel(gameObject, drawParams, drawParams.Graphics,
                gameObject.Facing, RampType.None, Color.White, true, gameObject.GetRemapColor(),
                drawPoint, yDrawPointWithoutCellHeight);
        }
    }
}
