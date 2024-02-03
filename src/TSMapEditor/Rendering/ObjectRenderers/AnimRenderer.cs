using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class AnimRenderer : ObjectRenderer<Animation>
    {
        public AnimRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Orange;

        protected override CommonDrawParams GetDrawParams(Animation gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.AnimType.ININame,
                MainImage = TheaterGraphics.AnimTextures[gameObject.AnimType.Index]
            };
        }

        protected override bool ShouldRenderReplacementText(Animation gameObject)
        {
            // Never draw this for animations
            return false;
        }

        protected override void Render(Animation gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            if (drawParams.MainImage == null)
                return;

            int frameIndex = gameObject.GetFrameIndex(drawParams.MainImage.GetFrameCount());
            if (gameObject.IsTurretAnim)
            {
                // Turret anims have their facing frames reversed
                byte facing = (byte)(255 - gameObject.Facing - 31);
                frameIndex = facing / (512 / drawParams.MainImage.GetFrameCount());
            }

            float alpha = 1.0f;

            // Translucency values don't seem to directly map into MonoGame alpha values,
            // this will need some investigating into
            switch (gameObject.AnimType.ArtConfig.Translucency)
            {
                case 75:
                    alpha = 0.1f;
                    break;
                case 50:
                    alpha = 0.2f;
                    break;
                case 25:
                    alpha = 0.5f;
                    break;
            }

            DrawShadow(gameObject, drawParams, drawPoint, heightOffset);

            DrawShapeImage(gameObject, drawParams, drawParams.MainImage,
                frameIndex, Color.White * alpha,
                gameObject.IsBuildingAnim, gameObject.GetRemapColor() * alpha,
                drawPoint, heightOffset);
        }

        protected override void DrawShadow(Animation gameObject, in CommonDrawParams drawParams, Point2D drawPoint, int heightOffset)
        {
            if (!Constants.DrawBuildingAnimationShadows && gameObject.IsBuildingAnim)
                return;

            int shadowFrameIndex = gameObject.GetShadowFrameIndex(drawParams.MainImage.GetFrameCount());

            if (gameObject.IsTurretAnim)
            {
                // Turret anims have their facing frames reversed
                byte facing = (byte)(255 - gameObject.Facing - 31);
                shadowFrameIndex += facing / (512 / drawParams.MainImage.GetFrameCount());
            }

            if (shadowFrameIndex > 0 && shadowFrameIndex < drawParams.MainImage.GetFrameCount())
            {
                DrawShapeImage(gameObject, drawParams, drawParams.MainImage, shadowFrameIndex,
                    new Color(0, 0, 0, 128), false, Color.White, drawPoint, heightOffset);
            }
        }
    }
}
