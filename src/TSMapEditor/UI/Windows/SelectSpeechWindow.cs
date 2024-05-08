using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectSpeechWindow : SelectObjectWindow<EvaSpeech>
    {
        public SelectSpeechWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectSpeechWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem?.Tag == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (EvaSpeech)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            IList<EvaSpeech> speechList = Constants.IsRA2YR ? map.Rules.Speeches.List : map.EditorConfig.Speeches.List;
            foreach (var evaSpeech in speechList)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = evaSpeech.ToString(), Tag = evaSpeech });
                if (evaSpeech == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
