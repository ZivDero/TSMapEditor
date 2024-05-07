using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using System;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectSoundWindow : SelectObjectWindow<int>
    {
        public SelectSoundWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectSoundWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem?.Tag == null)
            {
                SelectedObject = -1;
                return;
            }

            SelectedObject = (int)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (var sound in map.Rules.Sounds.List)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = sound.ToString(), Tag = sound.Index });
                if (sound.Index == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
