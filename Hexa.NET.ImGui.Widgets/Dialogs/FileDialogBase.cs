namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Text;
    using System.IO;
    using System.Numerics;
    using System.Text;

    // TODO: Search function

    public abstract class FileDialogBase : Dialog
    {
        private DirectoryInfo currentDir;
        private readonly List<FileSystemItem> entries = new();
        private string rootFolder;
        private string currentFolder;
        private readonly List<string> allowedExtensions = new();
        private RefreshFlags refreshFlags = RefreshFlags.Folders | RefreshFlags.Files;
        private readonly Stack<string> backHistory = new();
        private readonly Stack<string> forwardHistory = new();

        private bool breadcrumbs = true;
        private string searchString = string.Empty;
        private float widthDrives = 150;

        protected List<FileSystemItem> Entries => entries;

        public List<string> AllowedExtensions => allowedExtensions;

        public string RootFolder
        {
            get => rootFolder;
            set => rootFolder = value;
        }

        public string CurrentFolder
        {
            get => currentFolder;
            set
            {
                if (!Directory.Exists(value))
                {
                    return;
                }

                var old = currentFolder;
                currentFolder = value;
                OnSetCurrentFolder(old, value);
                OnCurrentFolderChanged(old, value);
            }
        }

        public DirectoryInfo CurrentDir => currentDir;

        public bool ShowHiddenFiles
        {
            get => (refreshFlags & RefreshFlags.IgnoreHidden) == 0;
            set
            {
                if (!value)
                {
                    refreshFlags |= RefreshFlags.IgnoreHidden;
                }
                else
                {
                    refreshFlags &= ~RefreshFlags.IgnoreHidden;
                }
                Refresh();
            }
        }

        public bool ShowFiles
        {
            get => (refreshFlags & RefreshFlags.Files) != 0;
            set
            {
                if (value)
                {
                    refreshFlags |= RefreshFlags.Files;
                }
                else
                {
                    refreshFlags &= ~RefreshFlags.Files;
                }
                Refresh();
            }
        }

        public bool ShowFolders
        {
            get => (refreshFlags & RefreshFlags.Folders) != 0;
            set
            {
                if (value)
                {
                    refreshFlags |= RefreshFlags.Folders;
                }
                else
                {
                    refreshFlags &= ~RefreshFlags.Folders;
                }
                Refresh();
            }
        }

        public bool OnlyAllowFilteredExtensions
        {
            get => (refreshFlags & RefreshFlags.OnlyAllowFilteredExtensions) != 0;
            set
            {
                if (value)
                {
                    refreshFlags |= RefreshFlags.OnlyAllowFilteredExtensions;
                }
                else
                {
                    refreshFlags &= ~RefreshFlags.OnlyAllowFilteredExtensions;
                }

                Refresh();
            }
        }

        public bool OnlyAllowFolders
        {
            get => !ShowFiles && ShowFolders;
            set
            {
                if (value)
                {
                    ShowFiles = false;
                    ShowFolders = true;
                }
                else
                {
                    ShowFiles = true;
                }
                Refresh();
            }
        }

        public override void Show()
        {
            base.Show();

            Refresh();
        }

        protected virtual void DrawMenuBar()
        {
            var style = WidgetManager.Style;
            if (ImGuiButton.TransparentButton($"{MaterialIcons.Home}"))
            {
                CurrentFolder = RootFolder;
            }
            ImGui.SameLine();
            if (ImGuiButton.TransparentButton($"{MaterialIcons.ArrowBack}"))
            {
                TryGoBack();
            }
            ImGui.SameLine();
            if (ImGuiButton.TransparentButton($"{MaterialIcons.ArrowForward}"))
            {
                TryGoForward();
            }
            ImGui.SameLine();
            if (ImGuiButton.TransparentButton($"{MaterialIcons.Refresh}"))
            {
                Refresh();
            }
            ImGui.SameLine();

            DrawBreadcrumb();

            ImGui.PushItemWidth(200);
            ImGui.InputTextWithHint("##Search", "Search ...", ref searchString, 1024);
            ImGui.PopItemWidth();
        }

        protected abstract bool IsSelected(FileSystemItem entry);

        protected abstract void OnClicked(FileSystemItem entry, bool shift, bool ctrl);

        protected virtual void OnDoubleClicked(FileSystemItem entry, bool shift, bool ctrl)
        {
            if (entry.IsFolder)
            {
                CurrentFolder = entry.Path;
            }
        }

        protected abstract void OnEnterPressed();

        protected virtual void OnEscapePressed()
        {
            Close(DialogResult.Cancel);
        }

        protected virtual void DrawExplorer()
        {
            Vector2 itemSpacing = ImGui.GetStyle().ItemSpacing;
            DrawMenuBar();

            float footerHeightToReserve = itemSpacing.Y + ImGui.GetFrameHeightWithSpacing();
            Vector2 avail = ImGui.GetContentRegionAvail();
            ImGui.Separator();

            if (ImGui.BeginChild("SidePanel"u8, new Vector2(widthDrives, -footerHeightToReserve), ImGuiWindowFlags.HorizontalScrollbar))
            {
                SidePanel();
            }
            ImGui.EndChild();

            ImGuiSplitter.VerticalSplitter("Vertical Splitter", ref widthDrives, 50, avail.X, -footerHeightToReserve, true);

            ImGui.SameLine();

            var cur = ImGui.GetCursorPos();
            ImGui.SetCursorPos(cur - itemSpacing);
            MainPanel(footerHeightToReserve);
            HandleInput();
        }

        protected virtual unsafe void DrawBreadcrumb()
        {
            var currentFolder = this.currentFolder;
            if (ImGuiBreadcrumb.Breadcrumb("Breadcrumb", ref currentFolder))
            {
                CurrentFolder = currentFolder;
            }
        }

        protected virtual unsafe bool MainPanel(float footerHeightToReserve)
        {
            if (currentDir.Exists)
            {
                var avail = ImGui.GetContentRegionAvail();
                ImGuiFileView<FileSystemItem>.FileView("0", new Vector2(avail.X + ImGui.GetStyle().WindowPadding.X, -footerHeightToReserve), entries, IsSelected, OnClicked, OnDoubleClicked, ContextMenu);
            }

            return false;
        }

        protected virtual void ContextMenu(FileSystemItem entry)
        {
        }

        protected virtual void SidePanel()
        {
            var currentFolder = this.currentFolder;
            if (ImGuiFileTreeView.FileTreeView("FileTreeView", default, ref currentFolder, rootFolder))
            {
                CurrentFolder = currentFolder;
            }
        }

        protected virtual void HandleInput()
        {
            bool focused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
            bool anyActive = ImGui.IsAnyItemActive() || ImGui.IsAnyItemFocused();

            // avoid handling input if any item is active, prevents issues with text input.
            if (!focused || anyActive)
            {
                return;
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                OnEscapePressed();
            }
            if (ImGui.IsKeyPressed(ImGuiKey.Enter))
            {
                OnEnterPressed();
            }
            if (ImGui.IsKeyPressed(ImGuiKey.F5))
            {
                Refresh();
            }

            if (ImGui.IsMouseClicked((ImGuiMouseButton)3))
            {
                TryGoBack();
            }
            if (ImGui.IsMouseClicked((ImGuiMouseButton)4))
            {
                TryGoForward();
            }
        }

        private unsafe void DisplaySize(long size)
        {
            byte* sizeBuffer = stackalloc byte[32];
            int sizeLength = Utf8Formatter.FormatByteSize(sizeBuffer, 32, size, true, 2);
            ImGui.TextDisabled(sizeBuffer);
        }

        protected bool FindRange(FileSystemItem entry, FileSystemItem lastEntry, out int startIndex, out int endIndex)
        {
            startIndex = Entries.IndexOf(lastEntry);

            if (startIndex == -1)
            {
                endIndex = -1; // setting endIndex to a valid number since it's an out parameter
                return false;
            }
            endIndex = Entries.IndexOf(entry);
            if (endIndex == -1)
            {
                return false;
            }

            // Swap the indexes if the start index is greater than the end index.
            if (startIndex > endIndex)
            {
                (startIndex, endIndex) = (endIndex, startIndex);
            }

            return true;
        }

        protected virtual void OnSetCurrentFolder(string oldFolder, string folder)
        {
            backHistory.Push(oldFolder);
            forwardHistory.Clear();
            Refresh();
        }

        protected virtual void OnCurrentFolderChanged(string old, string value)
        {
        }

        protected void SetInternal(string folder, bool refresh = true)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            var old = currentFolder;
            currentFolder = folder;
            OnCurrentFolderChanged(old, folder);
            if (refresh)
            {
                Refresh();
            }
        }

        public virtual void GoHome()
        {
            CurrentFolder = rootFolder;
        }

        public virtual void TryGoBack()
        {
            if (backHistory.TryPop(out var historyItem))
            {
                forwardHistory.Push(CurrentFolder);
                SetInternal(historyItem);
            }
        }

        public virtual void TryGoForward()
        {
            if (forwardHistory.TryPop(out var historyItem))
            {
                backHistory.Push(CurrentFolder);
                SetInternal(historyItem);
            }
        }

        public void ClearHistory()
        {
            forwardHistory.Clear();
            backHistory.Clear();
        }

        public virtual void Refresh()
        {
            currentDir = new DirectoryInfo(currentFolder);
            FileSystemHelper.Refresh(currentFolder, entries, refreshFlags, allowedExtensions, IconSelector, MaterialIcons.Folder);
            FileSystemHelper.ClearCache();
        }

        protected virtual string IconSelector(string path)
        {
            ReadOnlySpan<char> extension = Path.GetExtension(path.AsSpan());

            switch (extension)
            {
                case ".zip":
                    return $"{MaterialIcons.FolderZip}";

                case ".dds":
                case ".png":
                case ".jpg":
                case ".ico":
                    return $"{MaterialIcons.Image}";

                default:
                    return $"{MaterialIcons.Draft}"; ;
            }
        }
    }
}