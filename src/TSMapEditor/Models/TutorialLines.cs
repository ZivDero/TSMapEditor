﻿using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TSMapEditor.Models
{
    public struct TutorialLine
    {
        public TutorialLine(int id, string text)
        {
            ID = id;
            Text = text;
        }

        public int ID;
        public string Text;
    }

    public class TutorialLines
    {
        public TutorialLines(string iniPath, Action<Action> modifyEventCallback)
        {
            this.iniPath = iniPath;
            this.modifyEventCallback = modifyEventCallback;
            SetUpFSW();
            Read();
        }

        private readonly string iniPath;
        private readonly Action<Action> modifyEventCallback;

        private readonly object locker = new object();

        private bool callbackAdded = false;

        private FileSystemWatcher fsw;

        private void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            lock (locker)
            {
                if (callbackAdded)
                    return;

                callbackAdded = true;

                Logger.Log("Tutorial INI has been modified, adding callback to reload it.");

                modifyEventCallback(HandleFSW);
            }
        }

        private void HandleFSW()
        {
            lock (locker)
            {
                callbackAdded = false;

                tutorialLines.Clear();
                Read();
            }
        }

        private void SetUpFSW()
        {
            fsw = new FileSystemWatcher(Path.GetDirectoryName(iniPath));
            fsw.Filter = Path.GetFileName(iniPath);
            fsw.EnableRaisingEvents = true;
            fsw.Changed += Fsw_Changed;
        }

        public void ShutdownFSW()
        {
            fsw.EnableRaisingEvents = false;
            fsw.Changed -= Fsw_Changed;
            fsw.Dispose();
            fsw = null;
        }

        private Dictionary<int, string> tutorialLines = new Dictionary<int, string>();



        public List<TutorialLine> GetLines() => tutorialLines.Select(tl => new TutorialLine(tl.Key, tl.Value)).OrderBy(tl => tl.ID).ToList();

        /// <summary>
        /// Fetches a tutorial text line with the given ID.
        /// If the text line doesn't exist, returns an empty string.
        /// </summary>
        public string GetStringByIdOrEmptyString(int id)
        {
            if (tutorialLines.TryGetValue(id, out string value))
                return value;

            return string.Empty;
        }

        private void Read()
        {
            const string TutorialSectionName = "Tutorial";

            if (!File.Exists(iniPath))
                return;

            Logger.Log("Reading tutorial lines from " + iniPath);

            IniFile tutorialIni = new IniFile(iniPath);
            var keys = tutorialIni.GetSectionKeys(TutorialSectionName);
            if (keys == null)
                return;

            foreach (string key in keys)
            {
                int id = Conversions.IntFromString(key, -1);

                if (id > -1)
                {
                    tutorialLines.Add(id, tutorialIni.GetStringValue(TutorialSectionName, key, string.Empty));
                }
            }
        }
    }
}