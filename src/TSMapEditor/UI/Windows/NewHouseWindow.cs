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
            string houseTypeName = HouseName;

            var newHouse = new House(houseName)
            {
                Allies = houseName,
                Credits = 0,
                Edge = "West",
                IQ = 0,
                PercentBuilt = 100,
                PlayerControl = false,
                TechLevel = 10
            };

            HouseType newHouseType;

            if (Constants.UseCountries)
            {
                newHouseType = new HouseType(map.StandardHouseTypes.Find(c => c.ININame == ParentCountry), houseTypeName)
                {
                    ParentCountry = ParentCountry,
                    Index = map.GetHouseTypes(true).Last().Index + 1
                };

                newHouse.Color = newHouseType.Color;
                newHouse.XNAColor = newHouseType.XNAColor;
                newHouse.Country = houseTypeName;
            }
            else
            {
                var newColor = map.Rules.Colors.Find(c => c.Name == "Gold") ?? map.Rules.Colors[0];

                newHouseType = new HouseType(houseTypeName)
                {
                    Color = newColor.Name,
                    XNAColor = newColor.XNAColor,
                    Side = map.Rules.Sides[0],
                    Index = map.GetHouseTypes(true).Last().Index + 1
                };

                newHouse.Color = newColor.Name;
                newHouse.XNAColor = newColor.XNAColor;
                newHouse.ActsLike = 0;
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
