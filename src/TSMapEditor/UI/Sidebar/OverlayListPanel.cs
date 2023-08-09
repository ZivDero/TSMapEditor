﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using SharpDX.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Sidebar
{
    public class OverlayListPanel : XNAPanel, ISearchBoxContainer
    {
        public OverlayListPanel(WindowManager windowManager, EditorState editorState,
            Map map, TheaterGraphics theaterGraphics, ICursorActionTarget cursorActionTarget,
            OverlayPlacementAction overlayPlacementAction) : base(windowManager)
        {
            EditorState = editorState;
            Map = map;
            TheaterGraphics = theaterGraphics;
            this.cursorActionTarget = cursorActionTarget;
            this.overlayPlacementAction = overlayPlacementAction;
        }


        protected EditorState EditorState { get; }
        protected Map Map { get; }
        protected TheaterGraphics TheaterGraphics { get; }

        public XNASuggestionTextBox SearchBox { get; private set; }
        public TreeView ObjectTreeView { get; private set; }

        private readonly ICursorActionTarget cursorActionTarget;
        private readonly OverlayPlacementAction overlayPlacementAction;

        private OverlayCollectionPlacementAction overlayCollectionPlacementAction;

        public override void Initialize()
        {
            SearchBox = new XNASuggestionTextBox(WindowManager);
            SearchBox.Name = nameof(SearchBox);
            SearchBox.X = Constants.UIEmptySideSpace;
            SearchBox.Y = Constants.UIEmptyTopSpace;
            SearchBox.Width = Width - Constants.UIEmptySideSpace * 2;
            SearchBox.Height = Constants.UITextBoxHeight;
            SearchBox.Suggestion = "Search overlay... (CTRL + F)";
            AddChild(SearchBox);
            SearchBox.TextChanged += SearchBox_TextChanged;
            SearchBox.EnterPressed += SearchBox_EnterPressed;
            UIHelpers.AddSearchTipsBoxToControl(SearchBox);

            ObjectTreeView = new TreeView(WindowManager);
            ObjectTreeView.Name = nameof(ObjectTreeView);
            ObjectTreeView.Y = SearchBox.Bottom + Constants.UIVerticalSpacing;
            ObjectTreeView.Height = Height - ObjectTreeView.Y;
            ObjectTreeView.Width = Width;
            AddChild(ObjectTreeView);
            ObjectTreeView.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 222), 2, 2);

            base.Initialize();

            overlayCollectionPlacementAction = new OverlayCollectionPlacementAction(cursorActionTarget);
            ObjectTreeView.SelectedItemChanged += ObjectTreeView_SelectedItemChanged;
            overlayCollectionPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;
            overlayPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;

            InitOverlays();

            KeyboardCommands.Instance.NextSidebarNode.Triggered += NextSidebarNode_Triggered;
            KeyboardCommands.Instance.PreviousSidebarNode.Triggered += PreviousSidebarNode_Triggered;
        }

        private void ObjectTreeView_SelectedItemChanged(object sender, EventArgs e)
        {
            if (ObjectTreeView.SelectedNode == null)
                return;

            var tag = ObjectTreeView.SelectedNode.Tag;
            if (tag == null)
                return;

            if (tag is OverlayCollection collection)
            {
                overlayCollectionPlacementAction.OverlayCollection = collection;
                EditorState.CursorAction = overlayCollectionPlacementAction;
            }
            else if (tag is OverlayType overlayType)
            {
                overlayPlacementAction.OverlayType = overlayType;
                EditorState.CursorAction = overlayPlacementAction;
            }
            else
            {
                // Assume this to be the overlay removal entry
                overlayPlacementAction.OverlayType = null;
                EditorState.CursorAction = overlayPlacementAction;
            }
        }

        private void NextSidebarNode_Triggered(object sender, EventArgs e)
        {
            if (Enabled)
                ObjectTreeView.SelectNextNode();
        }

        private void PreviousSidebarNode_Triggered(object sender, EventArgs e)
        {
            if (Enabled)
                ObjectTreeView.SelectPreviousNode();
        }

        private void SearchBox_EnterPressed(object sender, EventArgs e)
        {
            ObjectTreeView.FindNode(SearchBox.Text, true);
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text) || SearchBox.Text == SearchBox.Suggestion)
                return;

            ObjectTreeView.FindNode(SearchBox.Text, false);
        }

        private void InitOverlays()
        {
            var categories = new List<TreeViewCategory>();

            categories.Add(new TreeViewCategory()
            {
                Text = "Erase Overlay",
                Tag = new object()
            });

            if (Map.EditorConfig.OverlayCollections.Count > 0)
            {
                var collectionsCategory = new TreeViewCategory() { Text = "Collections" };
                categories.Add(collectionsCategory);

                foreach (var collection in Map.EditorConfig.OverlayCollections)
                {
                    if (collection.Entries.Length == 0)
                        continue;

                    Texture2D texture = null;
                    var firstEntry = collection.Entries[0];
                    var textures = TheaterGraphics.OverlayTextures[firstEntry.OverlayType.Index];
                    if (textures != null)
                    {
                        var frames = textures.Frames;
                        int frameNumber = firstEntry.Frame;
                        if (firstEntry.OverlayType.Tiberium)
                            frameNumber = (frames.Length / 2) - 1;

                        if (frames != null && frames.Length > frameNumber)
                        {
                            var frame = frames[frameNumber];
                            if (frame != null)
                                texture = frame.Texture;
                        }
                    }

                    collectionsCategory.Nodes.Add(new TreeViewNode()
                    {
                        Text = collection.Name,
                        Tag = collection,
                        Texture = texture
                    });
                }
            }

            for (int i = 0; i < Map.Rules.OverlayTypes.Count; i++)
            {
                TreeViewCategory category = null;
                OverlayType overlayType = Map.Rules.OverlayTypes[i];

                if (Map.EditorConfig.EditorRulesIni.KeyExists("IgnoreTypes", overlayType.ININame))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(overlayType.EditorCategory))
                {
                    category = FindOrMakeCategory("Uncategorized", categories);
                }
                else
                {
                    category = FindOrMakeCategory(overlayType.EditorCategory, categories);
                }

                Texture2D texture = null;
                if (TheaterGraphics.OverlayTextures[i] != null)
                {
                    var frames = TheaterGraphics.OverlayTextures[i].Frames;
                    if (frames.Length > 0)
                    {
                        // Find the first valid frame and use that as our texture
                        int firstNotNullIndex = Array.FindIndex(frames, f => f != null);
                        if (firstNotNullIndex > -1)
                        {
                            texture = frames[firstNotNullIndex].Texture;
                        }
                    }
                }

                category.Nodes.Add(new TreeViewNode()
                {
                    Text = overlayType.Name + " (" + overlayType.ININame + ")",
                    Texture = texture,
                    Tag = overlayType
                });

                category.Nodes = category.Nodes.OrderBy(n => n.Text).ToList();
            }

            categories.ForEach(c => ObjectTreeView.AddCategory(c));
        }

        private TreeViewCategory FindOrMakeCategory(string categoryName, List<TreeViewCategory> categoryList)
        {
            var category = categoryList.Find(c => c.Text == categoryName);
            if (category != null)
                return category;

            category = new TreeViewCategory() { Text = categoryName };
            categoryList.Add(category);
            return category;
        }
    }
}
