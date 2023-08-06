﻿using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using TSMapEditor.Models;
using TSMapEditor.Scripts;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class RunScriptWindow : INItializableWindow
    {
        public RunScriptWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        public event EventHandler ScriptRun;

        private readonly Map map;

        private EditorListBox lbScriptFiles;

        private string scriptPath;

        public override void Initialize()
        {
            Name = nameof(RunScriptWindow);
            base.Initialize();

            lbScriptFiles = FindChild<EditorListBox>(nameof(lbScriptFiles));
            FindChild<EditorButton>("btnRunScript").LeftClick += BtnRunScript_LeftClick;
        }

        private void BtnRunScript_LeftClick(object sender, EventArgs e)
        {
            if (lbScriptFiles.SelectedItem == null)
                return;

            string filePath = (string)lbScriptFiles.SelectedItem.Tag;
            if (!File.Exists(filePath))
            {
                EditorMessageBox.Show(WindowManager, "Can't find file",
                    "The selected INI file doesn't exist! Maybe it was deleted?", MessageBoxButtons.OK);

                return;
            }

            scriptPath = filePath;

            string confirmation = ScriptRunner.GetDescriptionFromScript(filePath);
            if (confirmation == null)
            {
                confirmation = "The script has no description. Are you sure you wish to run it?";
            }

            confirmation = Renderer.FixText(confirmation, Constants.UIDefaultFont, Width).Text;

            var messageBox = EditorMessageBox.Show(WindowManager, "Are you sure?",
                confirmation, MessageBoxButtons.YesNo);
            messageBox.YesClickedAction = (_) => ApplyCode();
        }

        private void ApplyCode()
        {
            if (scriptPath == null)
                throw new InvalidOperationException("Pending script path is null!");

            string result = ScriptRunner.RunScript(map, scriptPath);
            result = Renderer.FixText(result, Constants.UIDefaultFont, Width).Text;

            EditorMessageBox.Show(WindowManager, "Result", result, MessageBoxButtons.OK);
            ScriptRun?.Invoke(this, EventArgs.Empty);
        }

        public void Open()
        {
            lbScriptFiles.Clear();

            string directoryPath = Path.Combine(Environment.CurrentDirectory, "Config", "Scripts");

            if (!Directory.Exists(directoryPath))
            {
                Logger.Log("WAE scipts directory not found!");
                EditorMessageBox.Show(WindowManager, "Error", "Scripts directory not found!\r\n\r\nExpected path: " + directoryPath, MessageBoxButtons.OK);
                return;
            }

            var iniFiles = Directory.GetFiles(directoryPath, "*.waescript");

            foreach (string filePath in iniFiles)
            {
                lbScriptFiles.AddItem(new XNAListBoxItem(Path.GetFileName(filePath)) { Tag = filePath });
            }

            Show();
        }
    }
}
