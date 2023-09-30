﻿using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectBridgeWindow : SelectObjectWindow<Bridge>
    {
        public SelectBridgeWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;
        public bool Success = false;

        public override void Initialize()
        {
            Name = nameof(SelectBridgeWindow);
            base.Initialize();

            FindChild<EditorButton>("btnSelect").LeftClick += BtnSelect_LeftClick;
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (Bridge)lbObjectList.SelectedItem.Tag;
        }

        protected void BtnSelect_LeftClick(object sender, EventArgs e)
        {
            Success = true;
        }

        public void Open()
        {
            Success = false;
            Open(null);
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (Bridge bridge in map.EditorConfig.Bridges)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{bridge.Name}", Tag = bridge });
            }
        }
    }
}