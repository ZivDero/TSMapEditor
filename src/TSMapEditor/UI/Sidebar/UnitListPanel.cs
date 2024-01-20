using Rampastring.XNAUI;
using System;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Sidebar
{
    /// <summary>
    /// A sidebar panel for listing units.
    /// </summary>
    public class UnitListPanel : ObjectListPanel
    {
        public UnitListPanel(WindowManager windowManager, EditorState editorState, Map map, TheaterGraphics theaterGraphics, ICursorActionTarget cursorActionTarget, bool isNaval) : base(windowManager, editorState, map, theaterGraphics)
        {
            unitPlacementAction = new UnitPlacementAction(cursorActionTarget, Keyboard);
            unitPlacementAction.ActionExited += UnitPlacementAction_ActionExited;

            this.isNaval = isNaval;
        }

        private void UnitPlacementAction_ActionExited(object sender, System.EventArgs e)
        {
            ObjectTreeView.SelectedNode = null;
        }

        private readonly bool isNaval;
        private readonly UnitPlacementAction unitPlacementAction;

        protected override void InitObjects()
        {
            Func<UnitType, bool> filterFunction = isNaval ?
                    u => u.Naval || 
                         u.MovementZone == "Amphibious" ||
                         u.MovementZone == "AmphibiousCrusher" ||
                         u.MovementZone == "AmphibiousDestroyer" ||
                         u.MovementZone == "Water" : 
                    u => !u.Naval ||
                         u.MovementZone == "Amphibious" ||
                         u.MovementZone == "AmphibiousCrusher" ||
                         u.MovementZone == "AmphibiousDestroyer";

            InitObjectsBase(Map.Rules.UnitTypes, TheaterGraphics.UnitTextures, filterFunction);
        }

        protected override void ObjectSelected()
        {
            if (ObjectTreeView.SelectedNode == null)
                return;

            unitPlacementAction.UnitType = (UnitType)ObjectTreeView.SelectedNode.Tag;
            EditorState.CursorAction = unitPlacementAction;
        }
    }
}
