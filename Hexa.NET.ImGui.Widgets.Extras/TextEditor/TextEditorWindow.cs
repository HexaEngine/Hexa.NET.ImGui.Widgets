﻿namespace Hexa.NET.ImGui.Widgets.Extras.TextEditor
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.ImGui.Widgets.Extras.TextEditor.Panels;
    using System.IO;
    using System.Numerics;

    /// <summary>
    /// A work in progress text editor with syntax highlighting and more.
    /// </summary>
    public class TextEditorWindow : ImWindow, IDisposable
    {
        private readonly List<TextEditorTab> tabs = new();
        private readonly List<SidePanel> sidePanels = new();
        private TextEditorTab? currentTab;

        public TextEditorWindow()
        {
            Flags = ImGuiWindowFlags.MenuBar;
            sidePanels.Add(new ExplorerSidePanel());
        }

        protected override string Name => "Text Editor";

        public IReadOnlyList<TextEditorTab> Tabs => tabs;

        public TextEditorTab? CurrentTab => currentTab;

        private void DrawMenuBar()
        {
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem($"{MaterialIcons.Create} New", "Ctrl+N"))
                    {
                        New();
                    }

                    if (ImGui.MenuItem($"{MaterialIcons.FileOpen} Open"))
                    {
                        OpenFileDialog openDialog = new();
                        openDialog.Show(DialogCallback);
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem($"{MaterialIcons.Save} Save", "Ctrl+S"))
                    {
                        Save();
                    }

                    if (ImGui.MenuItem($"{MaterialIcons.SaveAs} Save As"))
                    {
                        SaveFileDialog saveDialog = new();
                        saveDialog.Show(DialogCallback);
                    }

                    ImGui.Separator();

                    ImGui.BeginDisabled(currentTab == null);

                    if (ImGui.MenuItem($"{MaterialIcons.Close} Close"))
                    {
                        if (currentTab != null)
                        {
                            tabs.Remove(currentTab);
                            currentTab = null;
                        }
                    }

                    ImGui.EndDisabled();

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Edit"))
                {
                    ImGui.BeginDisabled(currentTab == null);

                    if (ImGui.MenuItem($"{MaterialIcons.Search} Search"))
                    {
                        currentTab?.ShowFind();
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem($"{MaterialIcons.RotateLeft} Undo", "Ctrl+Z"))
                    {
                        currentTab?.Undo();
                    }

                    if (ImGui.MenuItem($"{MaterialIcons.RotateRight} Redo", "Ctrl+Y"))
                    {
                        currentTab?.Redo();
                    }

                    ImGui.EndDisabled();

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }
        }

        private void DialogCallback(object? sender, DialogResult result)
        {
            if (result == DialogResult.Ok && sender is OpenFileDialog openDialog)
            {
                Open(openDialog.SelectedFile!);
            }
            if (result == DialogResult.Ok && sender is SaveFileDialog saveDialog)
            {
                SaveAs(saveDialog.SelectedFile!);
            }
        }

        public void Open(string path)
        {
            try
            {
                TextEditorTab tab = new(new(File.ReadAllText(path)), path);
                tabs.Add(tab);
            }
            catch (Exception ex)
            {
                //Logger.Error("Failed to load text file!");
                //Logger.Log(ex);
                MessageBox.Show("Failed to load text file!", ex.Message);
            }
        }

        public void New()
        {
            TextEditorTab tab = new("New File", new(string.Empty));
            tabs.Add(tab);
        }

        public void Save()
        {
            if (currentTab?.CurrentFile != null)
            {
                SaveAs(currentTab.CurrentFile);
            }
        }

        public unsafe void SaveAs(string path)
        {
            if (currentTab == null)
            {
                return;
            }
            try
            {
                File.WriteAllText(path, currentTab.Source.Text->ToString());
                currentTab.Source.Changed = false;
                currentTab.CurrentFile = path;
            }
            catch (Exception ex)
            {
                //Logger.Error("Failed to save text file!");
                //Logger.Log(ex);
                MessageBox.Show("Failed to save text file!", ex.Message);
            }
        }

        public override unsafe void DrawContent()
        {
            DrawMenuBar();
            HandleShortcuts();

            Vector2 cursor = ImGui.GetCursorPos();
            Vector2 avail = ImGui.GetContentRegionAvail();

            float actualSidePanelWidth = sidePanelCollapsed ? 0 : sidePanelWidth;

            if (ImGui.BeginTabBar("##TextEditor"))
            {
                Vector2 cursorTab = ImGui.GetCursorPos();
                for (int i = 0; i < tabs.Count; i++)
                {
                    var tab = tabs[i];
                    if (tab.IsFocused)
                    {
                        currentTab = tab;
                    }
                    if (!tab.IsOpen)
                    {
                        tab.Dispose();
                        tabs.RemoveAt(i);
                        if (tab == currentTab)
                        {
                            currentTab = null;
                        }

                        i--;
                    }

                    Vector2 size = new(avail.X - sideBarWidth - actualSidePanelWidth - resizeBarWidth, avail.Y - (cursorTab.Y - cursor.Y));
                    tab.Draw(size);
                }
                ImGui.EndTabBar();
            }

            ImGui.SetCursorPos(cursor + new Vector2(avail.X - sideBarWidth - actualSidePanelWidth - resizeBarWidth, 0));

            ImGui.InvisibleButton("ResizeBar", new Vector2(resizeBarWidth, avail.Y));

            if (ImGui.IsItemHovered())
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                sidePanelWidth -= ImGui.GetIO().MouseDelta.X;
                sidePanelWidth = Math.Max(sidePanelWidth, 0);
                sidePanelCollapsed = sidePanelWidth <= 100;
            }

            ImGui.SetCursorPos(cursor + new Vector2(avail.X - sideBarWidth - actualSidePanelWidth + resizeBarWidth, 0));
            DrawSidePanel();

            ImGui.SetCursorPos(cursor + new Vector2(avail.X - sideBarWidth + resizeBarWidth, 0));
            //DrawSideBar();
        }

        private unsafe void DrawSidePanel()
        {
            if (sidePanelCollapsed)
            {
                return;
            }

            if (activeSidePanel < 0 && activeSidePanel >= sidePanels.Count)
            {
                return;
            }

            ImGui.BeginChild("##SidePanel", new Vector2(sidePanelWidth, 0));
            var cursor = ImGui.GetCursorScreenPos();
            var avail = ImGui.GetContentRegionAvail();
            var max = cursor + new Vector2(sidePanelWidth, avail.Y);
            ImDrawList* drawList = ImGui.GetWindowDrawList();
            drawList->AddRectFilled(cursor, max, 0xff1c1c1c);

            sidePanels[activeSidePanel].Draw();

            ImGui.EndChild();
        }

        private const float resizeBarWidth = 2.0f;
        private const float sideBarWidth = 40f;
        private float sidePanelWidth = 0;
        private bool sidePanelCollapsed = false;
        private int activeSidePanel;

        private unsafe void DrawSideBar()
        {
            ImGui.BeginChild("##SideBar", new Vector2(sideBarWidth, 0));
            var cursor = ImGui.GetCursorScreenPos();
            var avail = ImGui.GetContentRegionAvail();
            var max = cursor + new Vector2(sideBarWidth, avail.Y);
            ImDrawList* drawList = ImGui.GetWindowDrawList();
            drawList->AddRectFilled(cursor, max, 0xff2c2c2c);

            for (int i = 0; i < sidePanels.Count; i++)
            {
                var sidePanel = sidePanels[i];
                if (ImGui.Button(sidePanel.Icon))
                {
                    activeSidePanel = i;
                    sidePanelWidth = sidePanelWidth <= 100 ? 200f : sidePanelWidth;
                    sidePanelCollapsed = false;
                }
            }
            ImGui.EndChild();
        }

        private void HandleShortcuts()
        {
            if (ImGuiP.Shortcut((int)(ImGuiKey.ModCtrl | ImGuiKey.N)))
            {
                New();
            }

            if (ImGuiP.Shortcut((int)(ImGuiKey.ModCtrl | ImGuiKey.S)))
            {
                Save();
            }

            if (ImGuiP.Shortcut((int)(ImGuiKey.ModCtrl | ImGuiKey.A)))
            {
                CurrentTab?.SelectAll();
            }

            if (ImGuiP.Shortcut((int)(ImGuiKey.ModCtrl | ImGuiKey.Z)))
            {
                CurrentTab?.Undo();
            }

            if (ImGuiP.Shortcut((int)(ImGuiKey.ModCtrl | ImGuiKey.Y)))
            {
                CurrentTab?.Redo();
            }

            if (ImGuiP.Shortcut((int)(ImGuiKey.ModCtrl | ImGuiKey.F)))
            {
                CurrentTab?.ShowFind();
            }
        }

        public override void Dispose()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].Dispose();
            }

            tabs.Clear();
            GC.SuppressFinalize(this);
        }
    }
}