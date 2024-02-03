using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class InfantryRenderer : ObjectRenderer<Infantry>
    {
        public InfantryRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Teal;

        protected override CommonDrawParams GetDrawParams(Infantry gameObject)
        {
            var mainImage = TheaterGraphics.InfantryTextures[gameObject.ObjectType.Index];
            string iniName = gameObject.ObjectType.ININame;

            return new CommonDrawParams()
            {
                IniName = iniName,
                MainImage = mainImage
            };
        }

        protected override void Render(Infantry gameObject, int heightOffset, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            switch (gameObject.SubCell)
            {
                case SubCell.Top:
                    drawPoint += new Point2D(0, Constants.CellSizeY / -4);
                    break;
                case SubCell.Bottom:
                    drawPoint += new Point2D(0, Constants.CellSizeY / 4);
                    break;
                case SubCell.Left:
                    drawPoint += new Point2D(Constants.CellSizeX / -4, 0);
                    break;
                case SubCell.Right:
                    drawPoint += new Point2D(Constants.CellSizeX / 4, 0);
                    break;
                case SubCell.Center:
                default:
                    break;
            }

            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject, drawParams, drawPoint, heightOffset);

            DrawShapeImage(gameObject, drawParams, drawParams.MainImage, 
                gameObject.GetFrameIndex(drawParams.MainImage.GetFrameCount()), 
                Color.White, true, gameObject.GetRemapColor(), drawPoint, heightOffset);
        }
    }
}
