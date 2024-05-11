using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// Cursor action for placing bridges.
    /// </summary>
    public class DrawCliffCursorAction : CursorAction
    {
        public DrawCliffCursorAction(ICursorActionTarget cursorActionTarget, CliffType cliffType) : base(cursorActionTarget)
        {
            this.cliffType = cliffType;
            ActionExited += UndoOnExit;
        }

        public override string GetName() => "Draw Cliff";

        public override bool HandlesKeyboardInput => true;

        public override bool DrawCellCursor => true;

        private readonly CliffType cliffType;

        private List<Point2D> cliffPath;
        private CliffSide cliffSide = CliffSide.Front;
        private DrawCliffMutation previewMutation;

        private readonly int randomSeed = Guid.NewGuid().GetHashCode();

        public override void OnActionEnter()
        {
            cliffPath = new List<Point2D>();

            base.OnActionEnter();
        }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(cellCoords, CursorActionTarget.Map) - cameraTopLeftPoint;

            cellTopLeftPoint = cellTopLeftPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            const string text = "Click on a cell to place a new vertex.\r\n\r\nENTER to confirm\r\nBackspace to go back one step\r\nTAB to change cliff side\r\nRight-click or ESC to exit";
            var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
            int x = cellTopLeftPoint.X - (int)(textDimensions.X - Constants.CellSizeX) / 2;

            Vector2 textPosition = new Vector2(x + 60, cellTopLeftPoint.Y - 150);

            Rectangle textBackgroundRectangle = new Rectangle((int)textPosition.X - Constants.UIEmptySideSpace,
                (int)textPosition.Y - Constants.UIEmptyTopSpace,
                (int)textDimensions.X + Constants.UIEmptySideSpace * 2,
                (int)textDimensions.Y + Constants.UIEmptyBottomSpace + Constants.UIEmptyTopSpace);

            Renderer.FillRectangle(textBackgroundRectangle, UISettings.ActiveSettings.PanelBackgroundColor);
            Renderer.DrawRectangle(textBackgroundRectangle, UISettings.ActiveSettings.PanelBorderColor);

            Renderer.DrawStringWithShadow(text, Constants.UIBoldFont, textPosition, Color.Yellow);

            Func<Point2D, Map, Point2D> getCellCenterPoint = Is2DMode ? CellMath.CellCenterPointFromCellCoords : CellMath.CellCenterPointFromCellCoords_3D;

            // Draw cliff path
            for (int i = 0; i < cliffPath.Count - 1; i++)
            {
                Point2D start = cliffPath[i];
                start = getCellCenterPoint(start, CursorActionTarget.Map) - cameraTopLeftPoint;

                Point2D end = cliffPath[i + 1];
                end = getCellCenterPoint(end, CursorActionTarget.Map) - cameraTopLeftPoint;


                Color color = Color.Goldenrod;
                int thickness = 3;

                Renderer.DrawLine(start.ToXNAVector(), end.ToXNAVector(), color, thickness);
            }
        }

        public override void OnKeyPressed(KeyPressEventArgs e)
        {
            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Escape)
            {
                ExitAction();

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Tab)
            {
                cliffSide = cliffSide == CliffSide.Front ? CliffSide.Back : CliffSide.Front;
                RedrawPreview();

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Back)
            {
                if (cliffPath.Count > 0)
                    cliffPath.RemoveAt(cliffPath.Count - 1);
                
                RedrawPreview();

                e.Handled = true;
            }
            else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Enter && cliffPath.Count >= 2)
            {
                previewMutation?.Undo();
                CursorActionTarget.MutationManager.PerformMutation(new DrawCliffMutation(CursorActionTarget.MutationTarget, cliffPath, cliffType, cliffSide, randomSeed));

                ExitAction();

                e.Handled = true;
            }
        }

        public override void LeftClick(Point2D cellCoords)
        {
            cliffPath.Add(cellCoords);
            RedrawPreview();
        }

        private void RedrawPreview()
        {
            previewMutation?.Undo();

            if (cliffPath.Count >= 2)
            {
                previewMutation = new DrawCliffMutation(MutationTarget, cliffPath, cliffType, cliffSide, randomSeed);
                previewMutation.Perform();
            }
            else
            {
                previewMutation = null;
            }
        }

        private void UndoOnExit(object sender, EventArgs e)
        {
            previewMutation?.Undo();
        }

        public override void LeftDown(Point2D cellCoords) => LeftClick(cellCoords);
    }
}
