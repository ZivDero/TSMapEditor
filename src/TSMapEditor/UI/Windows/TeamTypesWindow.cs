﻿using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class TaskForceEventArgs : EventArgs
    {
        public TaskForceEventArgs(TaskForce taskForce)
        {
            TaskForce = taskForce;
        }

        public TaskForce TaskForce { get; }
    }

    public class ScriptEventArgs : EventArgs
    {
        public ScriptEventArgs(Script script)
        {
            Script = script;
        }

        public Script Script { get; }
    }

    public class TeamTypesWindow : INItializableWindow
    {
        public TeamTypesWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public event EventHandler<TaskForceEventArgs> TaskForceOpened;
        public event EventHandler<ScriptEventArgs> ScriptOpened;
        public event EventHandler<TagEventArgs> TagOpened;

        private EditorListBox lbTeamTypes;
        private EditorTextBox tbName;
        private XNADropDown ddVeteranLevel;
        private XNADropDown ddHouse;
        private EditorNumberTextBox tbPriority;
        private EditorNumberTextBox tbMax;
        private EditorNumberTextBox tbTechLevel;
        private XNADropDown ddMindControlDecision;
        private EditorNumberTextBox tbTransportWaypoint;
        private EditorNumberTextBox tbGroup;
        private EditorNumberTextBox tbWaypoint;
        private EditorPopUpSelector selTaskForce;
        private EditorPopUpSelector selScript;
        private EditorPopUpSelector selTag;

        private TeamType editedTeamType;
        private List<XNACheckBox> checkBoxes = new List<XNACheckBox>();

        private SelectTaskForceWindow selectTaskForceWindow;
        private SelectScriptWindow selectScriptWindow;
        private SelectTagWindow selectTagWindow;

        public override void Initialize()
        {
            Name = nameof(TeamTypesWindow);
            base.Initialize();

            lbTeamTypes = FindChild<EditorListBox>(nameof(lbTeamTypes));
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            ddVeteranLevel = FindChild<XNADropDown>(nameof(ddVeteranLevel));
            ddHouse = FindChild<XNADropDown>(nameof(ddHouse));
            tbPriority = FindChild<EditorNumberTextBox>(nameof(tbPriority));
            tbMax = FindChild<EditorNumberTextBox>(nameof(tbMax));
            tbTechLevel = FindChild<EditorNumberTextBox>(nameof(tbTechLevel));
            ddMindControlDecision = FindChild<XNADropDown>(nameof(ddMindControlDecision));
            tbTransportWaypoint = FindChild<EditorNumberTextBox>(nameof(tbTransportWaypoint));
            tbGroup = FindChild<EditorNumberTextBox>(nameof(tbGroup));
            tbWaypoint = FindChild<EditorNumberTextBox>(nameof(tbWaypoint));
            selTaskForce = FindChild<EditorPopUpSelector>(nameof(selTaskForce));
            selScript = FindChild<EditorPopUpSelector>(nameof(selScript));
            selTag = FindChild<EditorPopUpSelector>(nameof(selTag));

            var panelBooleans = FindChild<EditorPanel>("panelBooleans");
            AddBooleanProperties(panelBooleans);

            lbTeamTypes.SelectedIndexChanged += LbTeamTypes_SelectedIndexChanged;

            FindChild<EditorButton>("btnNewTeamType").LeftClick += BtnNewTeamType_LeftClick;
            FindChild<EditorButton>("btnDeleteTeamType").LeftClick += BtnDeleteTeamType_LeftClick;
            FindChild<EditorButton>("btnCloneTeamType").LeftClick += BtnCloneTeamType_LeftClick;

            selectTaskForceWindow = new SelectTaskForceWindow(WindowManager, map);
            var taskForceDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTaskForceWindow);
            taskForceDarkeningPanel.Hidden += (s, e) => SelectionWindow_ApplyEffect(w => editedTeamType.TaskForce = w.SelectedObject, selectTaskForceWindow);

            selectScriptWindow = new SelectScriptWindow(WindowManager, map);
            var scriptDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectScriptWindow);
            scriptDarkeningPanel.Hidden += (s, e) => SelectionWindow_ApplyEffect(w => editedTeamType.Script = w.SelectedObject, selectScriptWindow);

            selectTagWindow = new SelectTagWindow(WindowManager, map);
            var tagDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTagWindow);
            tagDarkeningPanel.Hidden += (s, e) => SelectionWindow_ApplyEffect(w => editedTeamType.Tag = w.SelectedObject, selectTagWindow);

            selTaskForce.LeftClick += (s, e) => 
            {
                if (editedTeamType == null)
                    return;

                if (Keyboard.IsCtrlHeldDown() && editedTeamType.TaskForce != null)
                {
                    OpenTaskForce();
                }
                else
                {
                    selectTaskForceWindow.Open(editedTeamType.TaskForce);
                }
            };

            selScript.LeftClick += (s, e) =>
            {
                if (editedTeamType == null)
                    return;

                if (Keyboard.IsCtrlHeldDown() && editedTeamType.Script != null)
                {
                    OpenScript();
                }
                else
                {
                    selectScriptWindow.Open(editedTeamType.Script);
                }
            };

            FindChild<EditorButton>("btnOpenTaskForce").LeftClick += (s, e) => OpenTaskForce();
            FindChild<EditorButton>("btnOpenScript").LeftClick += (s, e) => OpenScript();
            FindChild<EditorButton>("btnOpenTag").LeftClick += (s, e) => OpenTag();

            selTag.LeftClick += (s, e) => { if (editedTeamType != null) selectTagWindow.Open(editedTeamType.Tag); };
        }

        private void OpenTaskForce()
        {
            if (editedTeamType == null || editedTeamType.TaskForce == null)
                return;

            TaskForceOpened?.Invoke(this, new TaskForceEventArgs(editedTeamType.TaskForce));
            PutOnBackground();
        }

        private void OpenScript()
        {
            if (editedTeamType == null || editedTeamType.Script == null)
                return;

            ScriptOpened?.Invoke(this, new ScriptEventArgs(editedTeamType.Script));
            PutOnBackground();
        }

        private void OpenTag()
        {
            if (editedTeamType == null || editedTeamType.Tag == null)
                return;

            TagOpened?.Invoke(this, new TagEventArgs(editedTeamType.Tag));
            PutOnBackground();
        }

        private void SelectionWindow_ApplyEffect<T>(Action<T> action, T window)
        {
            if (lbTeamTypes.SelectedItem == null || editedTeamType == null)
            {
                return;
            }

            action(window);
            EditTeamType(editedTeamType);
        }

        private void BtnNewTeamType_LeftClick(object sender, EventArgs e)
        {
            var teamType = new TeamType(map.GetNewUniqueInternalId()) { Name = "New TeamType" };
            map.EditorConfig.TeamTypeFlags.ForEach(flag => { if (flag.DefaultValue) teamType.EnableFlag(flag.Name); });
            map.TeamTypes.Add(teamType);
            ListTeamTypes();
            lbTeamTypes.SelectedIndex = map.TeamTypes.Count - 1;
            lbTeamTypes.ScrollToBottom();
        }

        private void BtnDeleteTeamType_LeftClick(object sender, EventArgs e)
        {
            if (editedTeamType == null)
                return;

            if (Keyboard.IsShiftHeldDown())
            {
                DeleteTeamType();
            }
            else
            {
                var messageBox = EditorMessageBox.Show(WindowManager,
                    "Confirm",
                    $"Are you sure you wish to delete '{editedTeamType.Name}'?" + Environment.NewLine + Environment.NewLine +
                    $"You'll need to manually fix any Triggers and AITriggers using the TeamType." + Environment.NewLine + Environment.NewLine +
                    "(You can hold Shift to skip this confirmation dialog.)",
                    MessageBoxButtons.YesNo);
                messageBox.YesClickedAction = _ => DeleteTeamType();
            }
        }

        private void DeleteTeamType()
        {
            if (editedTeamType == null)
                return;

            map.RemoveTeamType(editedTeamType);
            map.AITriggerTypes.ForEach(aitt =>
            {
                if (aitt.PrimaryTeam == editedTeamType)
                    aitt.PrimaryTeam = null;

                if (aitt.SecondaryTeam == editedTeamType)
                    aitt.SecondaryTeam = null;
            });
            ListTeamTypes();
            RefreshSelectedTeamType();
        }

        private void BtnCloneTeamType_LeftClick(object sender, EventArgs e)
        {
            if (lbTeamTypes.SelectedItem == null)
                return;

            map.TeamTypes.Add(((TeamType)lbTeamTypes.SelectedItem.Tag).Clone(map.GetNewUniqueInternalId()));
            ListTeamTypes();
            lbTeamTypes.SelectedIndex = map.TeamTypes.Count - 1;
            lbTeamTypes.ScrollToBottom();
        }

        private void LbTeamTypes_SelectedIndexChanged(object sender, EventArgs e) => RefreshSelectedTeamType();

        private void RefreshSelectedTeamType()
        {
            if (lbTeamTypes.SelectedItem == null)
            {
                editedTeamType = null;
                EditTeamType(null);
                return;
            }

            EditTeamType((TeamType)lbTeamTypes.SelectedItem.Tag);
        }

        private void AddBooleanProperties(EditorPanel panelBooleans)
        {
            int currentColumnRight = 0;
            int currentColumnX = Constants.UIEmptySideSpace;
            XNACheckBox previousCheckBoxOnColumn = null;

            foreach (var teamTypeFlag in map.EditorConfig.TeamTypeFlags)
            {
                var checkBox = new XNACheckBox(WindowManager);
                checkBox.Tag = teamTypeFlag.Name;
                checkBox.Text = teamTypeFlag.Name;
                panelBooleans.AddChild(checkBox);
                checkBoxes.Add(checkBox);

                if (previousCheckBoxOnColumn == null)
                {
                    checkBox.Y = Constants.UIEmptyTopSpace;
                    checkBox.X = currentColumnX;
                }
                else
                {
                    checkBox.Y = previousCheckBoxOnColumn.Bottom + Constants.UIVerticalSpacing;
                    checkBox.X = currentColumnX;

                    // Start new column
                    if (checkBox.Bottom > panelBooleans.Height - Constants.UIEmptyBottomSpace)
                    {
                        currentColumnX = currentColumnRight + Constants.UIHorizontalSpacing * 2;
                        checkBox.Y = Constants.UIEmptyTopSpace;
                        checkBox.X = currentColumnX;
                        currentColumnRight = 0;
                    }
                }

                previousCheckBoxOnColumn = checkBox;
                currentColumnRight = Math.Max(currentColumnRight, checkBox.Right);
            }
        }

        public void Open()
        {
            Show();
            ListHouses();
            ListTeamTypes();
        }

        private void ListHouses()
        {
            ddHouse.Items.Clear();
            map.GetHouseTypes().ForEach(ht => ddHouse.AddItem(ht.ININame, ht.XNAColor));
        }

        private void ListTeamTypes()
        {
            lbTeamTypes.Clear();

            foreach (var teamType in map.TeamTypes)
            {
                lbTeamTypes.AddItem(new XNAListBoxItem() { Text = teamType.Name, Tag = teamType, TextColor = teamType.GetXNAColor() });
            }
        }

        public void SelectTeamType(TeamType teamType)
        {
            int index = lbTeamTypes.Items.FindIndex(lbi => lbi.Tag == teamType);

            if (index > -1)
                lbTeamTypes.SelectedIndex = index;
        }

        private void EditTeamType(TeamType teamType)
        {
            tbName.TextChanged -= TbName_TextChanged;
            ddVeteranLevel.SelectedIndexChanged -= DdVeteranLevel_SelectedIndexChanged;
            ddHouse.SelectedIndexChanged -= DdHouse_SelectedIndexChanged;
            tbPriority.TextChanged -= TbPriority_TextChanged;
            tbMax.TextChanged -= TbMax_TextChanged;
            tbTechLevel.TextChanged -= TbTechLevel_TextChanged;
            ddMindControlDecision.SelectedIndexChanged -= DdMindControlDecision_SelectedIndexChanged;
            tbGroup.TextChanged -= TbGroup_TextChanged;
            tbWaypoint.TextChanged -= TbWaypoint_TextChanged;
            tbTransportWaypoint.TextChanged -= TbTransportWaypoint_TextChanged;
            checkBoxes.ForEach(chk => chk.CheckedChanged -= FlagCheckBox_CheckedChanged);

            editedTeamType = teamType;

            if (editedTeamType == null)
            {
                tbName.Text = string.Empty;
                ddVeteranLevel.SelectedIndex = -1;
                ddHouse.SelectedIndex = -1;
                tbPriority.Text = string.Empty;
                tbMax.Text = string.Empty;
                tbTechLevel.Text = string.Empty;
                ddMindControlDecision.SelectedIndex = -1;

                tbGroup.Text = string.Empty;
                tbWaypoint.Text = string.Empty;
                tbTransportWaypoint.Text = string.Empty;

                selTaskForce.Text = string.Empty;
                selTaskForce.Tag = null;

                selScript.Text = string.Empty;
                selScript.Tag = null;

                selTag.Text = string.Empty;
                selTag.Tag = null;

                checkBoxes.ForEach(chk => chk.Checked = false);

                return;
            }

            tbName.Text = editedTeamType.Name;
            ddVeteranLevel.SelectedIndex = editedTeamType.VeteranLevel - 1;
            ddHouse.SelectedIndex = ddHouse.Items.FindIndex(i => i.Text == (editedTeamType.HouseType == null ? "" : editedTeamType.HouseType.ININame));
            tbPriority.Value = editedTeamType.Priority;
            tbMax.Value = editedTeamType.Max;
            tbTechLevel.Value = editedTeamType.TechLevel;
            tbGroup.Value = editedTeamType.Group;
            tbWaypoint.Value = Helpers.GetWaypointNumberFromAlphabeticalString(editedTeamType.Waypoint);

            if (Constants.UseCountries)
            {
                ddMindControlDecision.SelectedIndex = editedTeamType.MindControlDecision ?? -1;
                tbTransportWaypoint.Value = Helpers.GetWaypointNumberFromAlphabeticalString(editedTeamType.TransportWaypoint);
            }
            
            if (editedTeamType.TaskForce != null)
                selTaskForce.Text = editedTeamType.TaskForce.Name + " (" + editedTeamType.TaskForce.ININame + ")";
            else
                selTaskForce.Text = string.Empty;

            if (editedTeamType.Script != null)
                selScript.Text = editedTeamType.Script.Name + " (" + editedTeamType.Script.ININame + ")";
            else
                selScript.Text = string.Empty;

            if (editedTeamType.Tag != null)
                selTag.Text = editedTeamType.Tag.Name + " (" + editedTeamType.Tag.ID + ")";
            else
                selTag.Text = string.Empty;

            checkBoxes.ForEach(chk => chk.Checked = editedTeamType.IsFlagEnabled((string)chk.Tag));

            tbName.TextChanged += TbName_TextChanged;
            ddVeteranLevel.SelectedIndexChanged += DdVeteranLevel_SelectedIndexChanged;
            ddHouse.SelectedIndexChanged += DdHouse_SelectedIndexChanged;
            tbPriority.TextChanged += TbPriority_TextChanged;
            tbMax.TextChanged += TbMax_TextChanged;
            tbTechLevel.TextChanged += TbTechLevel_TextChanged;
            ddMindControlDecision.SelectedIndexChanged += DdMindControlDecision_SelectedIndexChanged;
            tbGroup.TextChanged += TbGroup_TextChanged;
            tbWaypoint.TextChanged += TbWaypoint_TextChanged;
            tbTransportWaypoint.TextChanged += TbTransportWaypoint_TextChanged;
            checkBoxes.ForEach(chk => chk.CheckedChanged += FlagCheckBox_CheckedChanged);
        }

        private void FlagCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var checkBox = (XNACheckBox)sender;
            if (checkBox.Checked)
                editedTeamType.EnableFlag((string)checkBox.Tag);
            else
                editedTeamType.DisableFlag((string)checkBox.Tag);
        }

        private void TbWaypoint_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.Waypoint = Helpers.WaypointNumberToAlphabeticalString(tbWaypoint.Value);
        }

        private void TbTransportWaypoint_TextChanged(object sender, EventArgs e)
        {
            if (Constants.UseCountries)
            {
                editedTeamType.TransportWaypoint = Helpers.WaypointNumberToAlphabeticalString(tbTransportWaypoint.Value);
            }
        }

        private void TbGroup_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.Group = tbGroup.Value;
        }

        private void TbTechLevel_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.TechLevel = tbTechLevel.Value;
        }

        private void TbMax_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.Max = tbMax.Value;
        }

        private void TbPriority_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.Priority = tbPriority.Value;
        }

        private void DdHouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedTeamType.HouseType = map.GetHouseTypes()[ddHouse.SelectedIndex];
            lbTeamTypes.SelectedItem.TextColor = editedTeamType.HouseType.XNAColor;
        }

        private void DdVeteranLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedTeamType.VeteranLevel = ddVeteranLevel.SelectedIndex + 1;
        }

        private void DdMindControlDecision_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Constants.UseCountries)
            {
                editedTeamType.MindControlDecision = ddMindControlDecision.SelectedIndex;
            }
        }

        private void TbName_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.Name = tbName.Text;
            lbTeamTypes.SelectedItem.Text = tbName.Text;
        }
    }
}
