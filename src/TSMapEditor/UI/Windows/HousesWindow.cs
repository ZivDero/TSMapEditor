using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to configure houses of the map.
    /// </summary>
    public class HousesWindow : INItializableWindow
    {
        public HousesWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private XNADropDown ddHouseOfHumanPlayer;
        private XNADropDown ddMapMode;
        private EditorListBox lbHouseList;
        private EditorTextBox tbName;
        private XNADropDown ddIQ;
        private XNADropDown ddMapEdge;
        private XNADropDown ddSide;
        private EditorTextBox tbCountry;
        private XNADropDown ddActsLike;
        private XNADropDown ddColor;
        private XNADropDown ddTechnologyLevel;
        private XNADropDown ddPercentBuilt;
        private EditorTextBox tbAllies;
        private EditorNumberTextBox tbMoney;
        private XNACheckBox chkPlayerControl;

        private XNALabel lblStatsValue;

        private House editedHouse;

        private NewHouseWindow newHouseWindow;
        private EditCountryWindow editCountryWindow;

        public override void Initialize()
        {
            Name = nameof(HousesWindow);
            base.Initialize();

            ddHouseOfHumanPlayer = FindChild<XNADropDown>(nameof(ddHouseOfHumanPlayer));
            ddMapMode = FindChild<XNADropDown>(nameof(ddMapMode));
            lbHouseList = FindChild<EditorListBox>(nameof(lbHouseList));
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            ddIQ = FindChild<XNADropDown>(nameof(ddIQ));
            ddMapEdge = FindChild<XNADropDown>(nameof(ddMapEdge));
            ddSide = FindChild<XNADropDown>(nameof(ddSide));
            tbCountry = FindChild<EditorTextBox>(nameof(tbCountry));
            ddActsLike = FindChild<XNADropDown>(nameof(ddActsLike));
            ddColor = FindChild<XNADropDown>(nameof(ddColor));
            ddTechnologyLevel = FindChild<XNADropDown>(nameof(ddTechnologyLevel));
            ddPercentBuilt = FindChild<XNADropDown>(nameof(ddPercentBuilt));
            tbAllies = FindChild<EditorTextBox>(nameof(tbAllies));
            tbMoney = FindChild<EditorNumberTextBox>(nameof(tbMoney));
            chkPlayerControl = FindChild<XNACheckBox>(nameof(chkPlayerControl));

            lblStatsValue = FindChild<XNALabel>(nameof(lblStatsValue));
            lblStatsValue.Text = "";

            foreach (var side in map.Rules.Sides)
            {
                ddSide.AddItem(new XNADropDownItem() { Text = side, Tag = side });
            }

            for (int i = 0; i < map.GetHouses(true).Count; i++)
            {
                House house = map.GetHouses(true)[i];
                string houseString = $"{i} {house.ININame}";
                ddActsLike.AddItem(new XNADropDownItem() { Text = houseString, Tag = house.HouseType.Index });
            }

            foreach (RulesColor rulesColor in map.Rules.Colors.OrderBy(c => c.Name))
            {
                ddColor.AddItem(rulesColor.Name, rulesColor.XNAColor);
            }

            if (Constants.UseCountries)
            {
                ddActsLike.Visible = false;
                FindChild<XNALabel>("lblActsLike").Visible = false;
                ddSide.Visible = false;
                FindChild<XNALabel>("lblSide").Visible = false;
            }
            else
            {
                FindChild<EditorButton>("btnEditCountry").Visible = false;
                tbCountry.Visible = false;
                FindChild<XNALabel>("lblCountry").Visible = false;

                ddMapMode.AddItem("Cooperative (TS)");
            }

            if (!map.Basic.MultiplayerOnly)
                ddMapMode.SelectedIndex = 0;
            else if (map.TsCoop && ddMapMode.Items.Count >= 2)
                ddMapMode.SelectedIndex = 2;
            else
                ddMapMode.SelectedIndex = 1;

            tbName.InputEnabled = false;
            tbCountry.InputEnabled = false;

            FindChild<EditorButton>("btnAddHouse").LeftClick += BtnAddHouse_LeftClick;
            FindChild<EditorButton>("btnDeleteHouse").LeftClick += BtnDeleteHouse_LeftClick;
            FindChild<EditorButton>("btnStandardHouses").LeftClick += BtnStandardHouses_LeftClick;
            FindChild<EditorButton>("btnEditCountry").LeftClick += BtnEditCountry_LeftClick;
            FindChild<EditorButton>("btnMakeHouseRepairBuildings").LeftClick += BtnMakeHouseRepairBuildings_LeftClick;
            FindChild<EditorButton>("btnMakeHouseNotRepairBuildings").LeftClick += BtnMakeHouseNotRepairBuildings_LeftClick;

            ddHouseOfHumanPlayer.SelectedIndexChanged += DdHouseOfHumanPlayer_SelectedIndexChanged;
            ddMapMode.SelectedIndexChanged += DdMapMode_SelectedIndexChanged;
            lbHouseList.SelectedIndexChanged += LbHouseList_SelectedIndexChanged;

            newHouseWindow = new NewHouseWindow(WindowManager, map);
            editCountryWindow = new EditCountryWindow(WindowManager, map);

            var newHouseWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, newHouseWindow);
            newHouseWindowDarkeningPanel.Hidden += NewHouseWindowDarkeningPanel_Hidden;

            var editCountryWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, editCountryWindow);
            editCountryWindowDarkeningPanel.Hidden += EditCountryWindowDarkeningPanel_Hidden;
        }

        private void BtnEditCountry_LeftClick(object sender, EventArgs e)
        {
            if (editedHouse != null && editedHouse.HouseType != null && !editedHouse.IsPlayerHouse)
                editCountryWindow.Open(editedHouse.HouseType);
        }

        private void NewHouseWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (newHouseWindow.Success)
            {
                ListHouses();
            }
        }
        private void EditCountryWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            RefreshHouseInfo();
        }

        private void DdHouseOfHumanPlayer_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (ddHouseOfHumanPlayer.SelectedItem == null || ddHouseOfHumanPlayer.SelectedIndex == 0)
            {
                map.Basic.Player = null;
                return;
            }

            map.Basic.Player = ddHouseOfHumanPlayer.SelectedItem.Text;
        }

        private void DdMapMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (ddMapMode.SelectedIndex)
            {
                case 0:
                    map.Basic.MultiplayerOnly = false;
                    map.TsCoop = false;
                    break;
                case 1:
                default:
                    map.Basic.MultiplayerOnly = true;
                    map.TsCoop = false;
                    break;
                case 2:
                    map.Basic.MultiplayerOnly = true;
                    map.TsCoop = true;
                    break;
            }

            ListHouses();
        }

        private void BtnAddHouse_LeftClick(object sender, System.EventArgs e)
        {
            newHouseWindow.Open();
        }

        private void BtnDeleteHouse_LeftClick(object sender, System.EventArgs e)
        {
            if (editedHouse == null)
                return;

            // Player houses don't get deleted
            if (map.PlayerHouses.Contains(editedHouse))
                return;

            if (map.DeleteHouseType(editedHouse.HouseType))
                RefreshHouseInfo();

            if (map.DeleteHouse(editedHouse))
                ListHouses();
        }

        private void BtnStandardHouses_LeftClick(object sender, System.EventArgs e)
        {
            foreach (var house in map.StandardHouses)
                map.AddHouse(house);

            object selectedItem = null;
            if (lbHouseList.SelectedItem != null)
                selectedItem = lbHouseList.SelectedItem.Tag;

            ListHouses();
            lbHouseList.SelectedIndex = lbHouseList.Items.FindIndex(i => i.Tag == selectedItem);

            RefreshHouseInfo();
        }

        private void BtnMakeHouseRepairBuildings_LeftClick(object sender, EventArgs e)
        {
            if (editedHouse == null)
            {
                EditorMessageBox.Show(WindowManager, "No HouseType Selected", "Select a house first.", MessageBoxButtons.OK);
                return;
            }

            var dialog = EditorMessageBox.Show(WindowManager,
                "Are you sure?",
                "This enables the \"AI Repairs\" flag on all buildings of the house, which makes the AI repair them." + Environment.NewLine + Environment.NewLine +
                "No un-do is available. Do you wish to continue?", MessageBoxButtons.YesNo);
            dialog.YesClickedAction = _ => map.Structures.FindAll(s => s.Owner == editedHouse).ForEach(b => b.AIRepairable = true);
        }

        private void BtnMakeHouseNotRepairBuildings_LeftClick(object sender, EventArgs e)
        {
            if (editedHouse == null)
            {
                EditorMessageBox.Show(WindowManager, "No HouseType Selected", "Select a house first.", MessageBoxButtons.OK);
                return;
            }

            var dialog = EditorMessageBox.Show(WindowManager,
                "Are you sure?",
                "This disables the \"AI Repairs\" flag on all buildings of the house, which makes the AI NOT repair them." + Environment.NewLine + Environment.NewLine +
                "No un-do is available. Do you wish to continue?", MessageBoxButtons.YesNo);
            dialog.YesClickedAction = _ => map.Structures.FindAll(s => s.Owner == editedHouse).ForEach(b => b.AIRepairable = false);
        }

        private void LbHouseList_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (lbHouseList.SelectedItem == null)
            {
                editedHouse = null;
            }
            else
            {
                editedHouse = (House)lbHouseList.SelectedItem.Tag;
            }
            
            RefreshHouseInfo();
        }

        private void RefreshHouseInfo()
        {
            RefreshHouseStats();

            tbName.TextChanged -= TbName_TextChanged;
            ddIQ.SelectedIndexChanged -= DdIQ_SelectedIndexChanged;
            ddMapEdge.SelectedIndexChanged -= DdMapEdge_SelectedIndexChanged;
            ddSide.SelectedIndexChanged -= DdSide_SelectedIndexChanged;
            ddActsLike.SelectedIndexChanged -= DdActsLike_SelectedIndexChanged;
            ddColor.SelectedIndexChanged -= DdColor_SelectedIndexChanged;
            ddTechnologyLevel.SelectedIndexChanged -= DdTechnologyLevel_SelectedIndexChanged;
            ddPercentBuilt.SelectedIndexChanged -= DdPercentBuilt_SelectedIndexChanged;
            tbAllies.TextChanged -= TbAllies_TextChanged;
            tbMoney.TextChanged -= TbMoney_TextChanged;
            chkPlayerControl.CheckedChanged -= ChkPlayerControl_CheckedChanged;

            if (editedHouse == null)
            {
                tbName.Text = string.Empty;
                ddIQ.SelectedIndex = -1;
                ddMapEdge.SelectedIndex = -1;
                ddSide.SelectedIndex = -1;
                ddActsLike.SelectedIndex = -1;
                tbCountry.Text = string.Empty;
                ddColor.SelectedIndex = -1;
                ddTechnologyLevel.SelectedIndex = -1;
                ddPercentBuilt.SelectedIndex = -1;
                tbAllies.Text = string.Empty;
                tbMoney.Text = string.Empty;
                chkPlayerControl.Checked = false;
                lblStatsValue.Text = string.Empty;
                return;
            }
            else
            {
                tbName.Text = editedHouse.ININame;
                ddIQ.SelectedIndex = ddIQ.Items.FindIndex(item => Conversions.IntFromString(item.Text, -1) == editedHouse.IQ);
                ddMapEdge.SelectedIndex = ddMapEdge.Items.FindIndex(item => item.Text == editedHouse.Edge);
                ddSide.SelectedIndex = map.Rules.Sides.FindIndex(s => s == editedHouse.HouseType.Side);

                if (Constants.UseCountries)
                {
                    tbCountry.Text = editedHouse.Country;
                    tbCountry.TextColor = editedHouse.HouseType.XNAColor;
                }
                else
                {
                    if (editedHouse.ActsLike < map.GetHouses(true).Count)
                        ddActsLike.SelectedIndex = editedHouse.ActsLike ?? -1;
                    else
                        ddActsLike.SelectedIndex = -1;
                }

                ddColor.SelectedIndex = ddColor.Items.FindIndex(item => item.Text == editedHouse.Color);
                ddTechnologyLevel.SelectedIndex = ddTechnologyLevel.Items.FindIndex(item => Conversions.IntFromString(item.Text, -1) == editedHouse.TechLevel);
                ddPercentBuilt.SelectedIndex = ddPercentBuilt.Items.FindIndex(item => Conversions.IntFromString(item.Text, -1) == editedHouse.PercentBuilt);
                tbAllies.Text = editedHouse.Allies ?? string.Empty;
                tbMoney.Value = editedHouse.Credits;
                chkPlayerControl.Checked = editedHouse.PlayerControl;
            }

            tbName.TextChanged += TbName_TextChanged;
            ddIQ.SelectedIndexChanged += DdIQ_SelectedIndexChanged;
            ddMapEdge.SelectedIndexChanged += DdMapEdge_SelectedIndexChanged;
            ddSide.SelectedIndexChanged += DdSide_SelectedIndexChanged;
            ddActsLike.SelectedIndexChanged += DdActsLike_SelectedIndexChanged;
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;
            ddTechnologyLevel.SelectedIndexChanged += DdTechnologyLevel_SelectedIndexChanged;
            ddPercentBuilt.SelectedIndexChanged += DdPercentBuilt_SelectedIndexChanged;
            tbAllies.TextChanged += TbAllies_TextChanged;
            tbMoney.TextChanged += TbMoney_TextChanged;
            chkPlayerControl.CheckedChanged += ChkPlayerControl_CheckedChanged;
        }

        private void TbName_TextChanged(object sender, System.EventArgs e)
        {
            editedHouse.ININame = tbName.Text;
            ListHouses();
        }

        private void DdIQ_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.IQ = Conversions.IntFromString(ddIQ.SelectedItem.Text, -1);
        }

        private void DdMapEdge_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.Edge = ddMapEdge.SelectedItem?.Text;
        }

        private void DdSide_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.HouseType.Side = (string)ddSide.SelectedItem.Tag;
        }

        private void DdActsLike_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.ActsLike = ddActsLike.SelectedIndex;
        }

        private void DdColor_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.Color = ddColor.SelectedItem.Text;
            editedHouse.XNAColor = ddColor.SelectedItem.TextColor.Value;
            map.HouseColorUpdated(editedHouse);

            // For TS Houses change the HouseType color as well, 
            if (!Constants.UseCountries)
            {
                editedHouse.HouseType.Color = ddColor.SelectedItem.Text;
                editedHouse.HouseType.XNAColor = ddColor.SelectedItem.TextColor.Value;
                map.HouseTypeColorUpdated(editedHouse.HouseType);
            }

            ListHouses();
        }

        private void DdTechnologyLevel_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.TechLevel = Conversions.IntFromString(ddTechnologyLevel.SelectedItem.Text, -1);
        }

        private void DdPercentBuilt_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            editedHouse.PercentBuilt = Conversions.IntFromString(ddPercentBuilt.SelectedItem.Text, 100);
        }

        private void TbAllies_TextChanged(object sender, System.EventArgs e)
        {
            editedHouse.Allies = tbAllies.Text;
        }

        private void TbMoney_TextChanged(object sender, System.EventArgs e)
        {
            editedHouse.Credits = tbMoney.Value;
        }

        private void ChkPlayerControl_CheckedChanged(object sender, System.EventArgs e)
        {
            editedHouse.PlayerControl = chkPlayerControl.Checked;
        }

        public void Open()
        {
            Show();
            ListHouses();
        }

        private void ListHouses()
        {
            object selectedItem = null;
            if (lbHouseList.SelectedItem != null)
                selectedItem = lbHouseList.SelectedItem.Tag;

            lbHouseList.Clear();
            ddHouseOfHumanPlayer.Items.Clear();

            ddHouseOfHumanPlayer.AddItem("None");

            ddActsLike.Items.Clear();
            foreach (House house in map.GetHouses(false, true))
            {
                lbHouseList.AddItem(
                    new XNAListBoxItem
                    {
                        Text = house.ININame,
                        TextColor = house.XNAColor,
                        Tag = house
                    }
                );

                ddActsLike.AddItem(new XNADropDownItem() { Text = house.ID.ToString(CultureInfo.InvariantCulture) + " " + house.ININame, Tag = house.ID });
            }

            lbHouseList.SelectedIndex = lbHouseList.Items.FindIndex(i => i.Tag == selectedItem);

            ddHouseOfHumanPlayer.Items.Clear();
            ddHouseOfHumanPlayer.AddItem("None");

            foreach (House house in map.GetHouses(true, true))
                ddHouseOfHumanPlayer.AddItem(house.ININame, house.XNAColor);

            ddHouseOfHumanPlayer.SelectedIndex = map.Houses.FindIndex(h => h.ININame == map.Basic.Player) + 1;
        }

        private void RefreshHouseStats()
        {
            if (editedHouse == null)
            {
                lblStatsValue.Text = "";
                return;
            }

            string stats = "Power: " + map.Structures.Aggregate(0, (value, structure) => 
            {
                if (structure.Owner == editedHouse)
                    return value + structure.ObjectType.Power;

                return value;
            });

            lblStatsValue.Text = stats;
        }
    }
}
