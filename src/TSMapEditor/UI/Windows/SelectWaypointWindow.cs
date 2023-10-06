using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using SharpDX.Mathematics.Interop;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectWaypointWindow : SelectObjectWindow<Waypoint>
    {
        public SelectWaypointWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectWaypointWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (Waypoint)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            lbObjectList.AddItem(new XNAListBoxItem()
            {
                Text = "-1 (None)",
                Tag = new Waypoint()
                {
                    Identifier = -1
                }
            });

            foreach (Waypoint waypoint in map.Waypoints)
            {
                lbObjectList.AddItem(new XNAListBoxItem()
                {
                    Text = $"{waypoint.Identifier} ({Helpers.WaypointNumberToAlphabeticalString(waypoint.Identifier)})",
                    Tag = waypoint
                });

                if (waypoint == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}