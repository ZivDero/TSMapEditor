using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that prompts the user for the name and parent country of the new house.
    /// </summary>
    public class NewHouseWindow : INItializableWindow
    {
        public NewHouseWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private EditorTextBox tbHouseName;
        private XNADropDown ddParentCountry;

        public string HouseName { get; set; }
        public string ParentCountry { get; set; }
        public bool Success { get; set; }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(NewHouseWindow);
            base.Initialize();

            tbHouseName = FindChild<EditorTextBox>(nameof(tbHouseName));
            ddParentCountry = FindChild<XNADropDown>(nameof(ddParentCountry));

            tbHouseName.TextChanged += TbHouseName_TextChanged;
            ddParentCountry.SelectedIndexChanged += DdParentCountry_SelectedIndexChanged;
            FindChild<EditorButton>("btnAdd").LeftClick += BtnAdd_LeftClick;

            if (!Constants.UseCountries)
            {
                ddParentCountry.Visible = false;
                FindChild<XNALabel>("lblParentCountry").Visible = false;
            }
        }

        private void DdParentCountry_SelectedIndexChanged(object sender, EventArgs e)
        {
            ParentCountry = ddParentCountry.SelectedItem.Text;
        }

        private void TbHouseName_TextChanged(object sender, EventArgs e)
        {
            HouseName = tbHouseName.Text;
        }

        private void BtnAdd_LeftClick(object sender, EventArgs e)
        {
            string houseName = Constants.UseCountries ? $"{HouseName} House" : HouseName;
            string countryName = HouseName;

            var newHouse = new House(houseName)
            {
                Allies = houseName,
                Credits = 0,
                Edge = "North",
                IQ = 0,
                PercentBuilt = 100,
                PlayerControl = false,
                TechLevel = 10
            };

            HouseType newHouseType;

            if (Constants.UseCountries)
            {
                newHouseType = new HouseType(map.StandardHouseTypes.Find(c => c.ININame == ParentCountry), countryName)
                {
                    ParentCountry = ParentCountry,
                    Multiplay = false,
                    MultiplayPassive = true,
                    Index = map.GetHouseTypes(true).Last().Index + 1
                };

                newHouse.Country = countryName;
                newHouse.Color = newHouseType.Color;
                newHouse.XNAColor = newHouseType.XNAColor;
            }
            else
            {
                var newColor = map.Rules.Colors.Find(c => c.Name == "Gold");
                if (newColor == null)
                    newColor = map.Rules.Colors[0];

                newHouse.Color = newColor.Name;
                newHouse.XNAColor = newColor.XNAColor;
                newHouse.ActsLike = 0;
                newHouse.Side = map.Rules.Sides[0];

                newHouseType = new HouseType(countryName)
                {
                    Color = newHouse.Color,
                    XNAColor = newHouse.XNAColor,
                    Side = newHouse.Side,
                    Index = map.GetHouseTypes(true).Last().Index + 1
                };
            }

            newHouse.HouseType = newHouseType;

            map.AddHouse(newHouse);
            map.AddHouseType(newHouseType);

            Success = true;

            Hide();
        }

        private void ListParentCountries()
        {
            ddParentCountry.Items.Clear();
            map.StandardHouseTypes.ForEach(h => ddParentCountry.AddItem(h.ININame, h.XNAColor));
        }

        public void Open()
        {
            Show();
            ListParentCountries();
            ddParentCountry.SelectedIndex = 0;

            tbHouseName.Text = "NewHouse";

            HouseName = "NewHouse";
            Success = false;
        }
    }
}
