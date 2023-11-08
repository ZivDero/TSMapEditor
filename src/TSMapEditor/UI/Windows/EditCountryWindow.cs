using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
using System.Reflection;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that prompts the user for the name and parent country of the new house.
    /// </summary>
    public class EditCountryWindow : INItializableWindow
    {
        public EditCountryWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private EditorTextBox tbName;
        private XNADropDown ddParentCountry;
        private EditorTextBox tbSuffix;
        private EditorTextBox tbPrefix;
        private XNADropDown ddColor;
        private XNADropDown ddSide;
        private EditorListBox lbMultipliers;
        private EditorNumberTextBox tbSelectedMultiplier;
        private XNACheckBox chkSmartAI;
        private XNACheckBox chkMultiplay;
        private XNACheckBox chkMultiplayPassive;
        private XNACheckBox chkWallOwner;

        private readonly Map map;

        private HouseType editedCountry { get; set; }

        public override void Initialize()
        {
            Name = nameof(EditCountryWindow);
            base.Initialize();

            tbName = FindChild<EditorTextBox>(nameof(tbName));
            ddParentCountry = FindChild<XNADropDown>(nameof(ddParentCountry));
            tbSuffix = FindChild<EditorTextBox>(nameof(tbSuffix));
            tbPrefix = FindChild<EditorTextBox>(nameof(tbPrefix));
            ddColor = FindChild<XNADropDown>(nameof(ddColor));
            ddSide = FindChild<XNADropDown>(nameof(ddSide));
            lbMultipliers = FindChild<EditorListBox>(nameof(lbMultipliers));
            tbSelectedMultiplier = FindChild<EditorNumberTextBox>(nameof(tbSelectedMultiplier));
            chkSmartAI = FindChild<XNACheckBox>(nameof(chkSmartAI));
            chkMultiplay = FindChild<XNACheckBox>(nameof(chkMultiplay));
            chkMultiplayPassive = FindChild<XNACheckBox>(nameof(chkMultiplayPassive));
            chkWallOwner = FindChild<XNACheckBox>(nameof(chkWallOwner));

            tbName.InputEnabled = false;
            tbSuffix.AllowComma = false;
            tbPrefix.AllowComma = false;
            tbSelectedMultiplier.AllowDecimals = true;

            for (int i = 0; i < map.Rules.Sides.Count; i++)
            {
                string sideName = map.Rules.Sides[i];
                string sideString = $"{i} {sideName}";
                ddSide.AddItem(new XNADropDownItem() { Text = sideString, Tag = sideName });
            }

            foreach (var country in map.StandardHouseTypes)
                ddParentCountry.AddItem(new XNADropDownItem() { Text = country.ININame, Tag = country.ININame, TextColor = country.XNAColor});

            foreach (RulesColor rulesColor in map.Rules.Colors.OrderBy(c => c.Name))
                ddColor.AddItem(rulesColor.Name, rulesColor.XNAColor);

            foreach (var property in typeof(HouseType).GetProperties())
            {
                if (property.Name.EndsWith("Mult") || property.Name == "ROF" || property.Name == "Firepower")
                    lbMultipliers.AddItem(new XNAListBoxItem() { Text = property.Name, Tag = property });
            }

            tbSelectedMultiplier.DoubleDefaultValue = 1.0;
            tbName.InputEnabled = false;

            ddParentCountry.SelectedIndexChanged += DdParentCountry_SelectedIndexChanged;
            tbSuffix.TextChanged += TbSuffix_TextChanged;
            tbPrefix.TextChanged += TbPrefix_TextChanged;
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;
            ddSide.SelectedIndexChanged += DdSide_SelectedIndexChanged;
            lbMultipliers.SelectedIndexChanged += LbMultipliers_SelectedIndexChanged;
            tbSelectedMultiplier.TextChanged += TbSelectedMultiplier_TextChanged;
            chkSmartAI.CheckedChanged += ChkSmartAI_CheckedChanged;
            chkMultiplay.CheckedChanged += ChkMultiplay_CheckedChanged;
            chkMultiplayPassive.CheckedChanged += ChkMultiplayPassive_CheckedChanged;
            chkWallOwner.CheckedChanged += ChkWallOwner_CheckedChanged;
        }

        private void ChkWallOwner_CheckedChanged(object sender, EventArgs e)
        {
            editedCountry.WallOwner = chkWallOwner.Checked;
            CheckAddStandardCountry(editedCountry);
        }

        private void ChkMultiplayPassive_CheckedChanged(object sender, EventArgs e)
        {
            editedCountry.MultiplayPassive = chkMultiplayPassive.Checked;
            CheckAddStandardCountry(editedCountry);
        }

        private void ChkMultiplay_CheckedChanged(object sender, EventArgs e)
        {
            editedCountry.Multiplay = chkMultiplay.Checked;
            CheckAddStandardCountry(editedCountry);
        }

        private void ChkSmartAI_CheckedChanged(object sender, EventArgs e)
        {
            editedCountry.SmartAI = chkSmartAI.Checked;
            CheckAddStandardCountry(editedCountry);
        }

        private void TbSelectedMultiplier_TextChanged(object sender, EventArgs e)
        {
            var property = (PropertyInfo)lbMultipliers.SelectedItem.Tag;
            var safeValue = (float)tbSelectedMultiplier.DoubleValue;
            property.SetValue(editedCountry, safeValue);
            CheckAddStandardCountry(editedCountry);
        }

        private void LbMultipliers_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbSelectedMultiplier.TextChanged -= TbSelectedMultiplier_TextChanged;

            var property = (PropertyInfo)lbMultipliers.SelectedItem.Tag;
            var propertyValue = (float?)property.GetValue(editedCountry);
            tbSelectedMultiplier.Text = propertyValue != null ? propertyValue.ToString() : string.Empty;

            tbSelectedMultiplier.TextChanged += TbSelectedMultiplier_TextChanged;
        }

        private void DdSide_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedCountry.Side = (string)ddSide.SelectedItem.Tag;
            CheckAddStandardCountry(editedCountry);
        }

        private void DdColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedCountry.Color = ddColor.SelectedItem.Text;
            editedCountry.XNAColor = ddColor.SelectedItem.TextColor.Value;
            map.HouseTypeColorUpdated(editedCountry);
            CheckAddStandardCountry(editedCountry);
        }

        private void TbPrefix_TextChanged(object sender, EventArgs e)
        {
            editedCountry.Prefix = tbPrefix.Text;
            CheckAddStandardCountry(editedCountry);
        }

        private void TbSuffix_TextChanged(object sender, EventArgs e)
        {
            editedCountry.Suffix = tbSuffix.Text;
            CheckAddStandardCountry(editedCountry);
        }

        private void DdParentCountry_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedCountry.ParentCountry = (string)ddParentCountry.SelectedItem.Tag;
            CheckAddStandardCountry(editedCountry);
        }

        private void LoadCountryInfo()
        {
            ddParentCountry.SelectedIndexChanged -= DdParentCountry_SelectedIndexChanged;
            tbSuffix.TextChanged -= TbSuffix_TextChanged;
            tbPrefix.TextChanged -= TbPrefix_TextChanged;
            ddColor.SelectedIndexChanged -= DdColor_SelectedIndexChanged;
            ddSide.SelectedIndexChanged -= DdSide_SelectedIndexChanged;
            lbMultipliers.SelectedIndexChanged -= LbMultipliers_SelectedIndexChanged;
            tbSelectedMultiplier.TextChanged -= TbSelectedMultiplier_TextChanged;
            chkSmartAI.CheckedChanged -= ChkSmartAI_CheckedChanged;
            chkMultiplay.CheckedChanged -= ChkMultiplay_CheckedChanged;
            chkMultiplayPassive.CheckedChanged -= ChkMultiplayPassive_CheckedChanged;
            chkWallOwner.CheckedChanged -= ChkWallOwner_CheckedChanged;

            int parentCountryIndex = map.StandardHouseTypes.ToList().FindIndex(c => c.ININame == editedCountry.ParentCountry);
            ddParentCountry.SelectedIndex = parentCountryIndex;
            ddParentCountry.AllowDropDown = parentCountryIndex != -1;

            tbName.Text = editedCountry.ININame;
            tbSuffix.Text = editedCountry.Suffix;
            tbPrefix.Text = editedCountry.Prefix;
            ddColor.SelectedIndex = ddColor.Items.FindIndex(item => item.Text == editedCountry.Color);
            ddSide.SelectedIndex = map.Rules.Sides.FindIndex(s => s == editedCountry.Side);
            lbMultipliers.SelectedIndex = -1;
            tbSelectedMultiplier.Text = string.Empty;
            chkSmartAI.Checked = editedCountry.SmartAI ?? false;
            chkMultiplay.Checked = editedCountry.Multiplay ?? false;
            chkMultiplayPassive.Checked = editedCountry.MultiplayPassive ?? false;
            chkWallOwner.Checked = editedCountry.WallOwner ?? false;

            ddParentCountry.SelectedIndexChanged += DdParentCountry_SelectedIndexChanged;
            tbSuffix.TextChanged += TbSuffix_TextChanged;
            tbPrefix.TextChanged += TbPrefix_TextChanged;
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;
            ddSide.SelectedIndexChanged += DdSide_SelectedIndexChanged;
            lbMultipliers.SelectedIndexChanged += LbMultipliers_SelectedIndexChanged;
            tbSelectedMultiplier.TextChanged += TbSelectedMultiplier_TextChanged;
            chkSmartAI.CheckedChanged += ChkSmartAI_CheckedChanged;
            chkMultiplay.CheckedChanged += ChkMultiplay_CheckedChanged;
            chkMultiplayPassive.CheckedChanged += ChkMultiplayPassive_CheckedChanged;
            chkWallOwner.CheckedChanged += ChkWallOwner_CheckedChanged;
        }

        private void CheckAddStandardCountry(HouseType country)
        {
            if (map.StandardHouseTypes.Contains(editedCountry))
                map.AddHouseType(country);
        }

        public void Open(HouseType editedCountry)
        {
            this.editedCountry = editedCountry;
            LoadCountryInfo();

            Show();
        }
    }
}
