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
            var mainModel = TheaterGraphics.AircraftModels[gameObject.ObjectType.Index];
            string iniName = gameObject.ObjectType.ININame;

            return new CommonDrawParams()
            {
                IniName = iniName,
                MainVoxel = mainModel
            };
        }

        protected override void Render(Aircraft gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            DrawVoxelModel(gameObject, drawParams, drawParams.MainVoxel,
                gameObject.Facing, RampType.None, Color.White, true, gameObject.GetRemapColor(),
                drawPoint, heightOffset);
        }
    }
}
