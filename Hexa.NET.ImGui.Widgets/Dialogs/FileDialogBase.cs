using Kitty.UI.Dialogs;

namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Text;
    using HexaEngine.Editor;
    using System.IO;
    using System.Numerics;
    using System.Text;

    public abstract class FileDialogBase : DialogBase
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

        public void DrawMenuBar()
        {
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

        protected abstract void OnDoubleClicked(FileSystemItem entry, bool shift, bool ctrl);

        protected abstract void OnEnterPressed();

        protected virtual void OnEscapePressed()
        {
            Close(DialogResult.Cancel);
        }

        protected void DrawExplorer()
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

            ImGuiSplitter.VerticalSplitter("", ref widthDrives, 50, avail.X, -footerHeightToReserve, true);

            ImGui.SameLine();

            var cur = ImGui.GetCursorPos();
            ImGui.SetCursorPos(cur - itemSpacing);
            MainPanel(footerHeightToReserve);
            HandleInput();
        }

        protected unsafe void DrawBreadcrumb()
        {
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 cursor = ImGui.GetCursorPos();
            Vector2 avail = ImGui.GetContentRegionAvail();
            Vector2 spacing = ImGui.GetStyle().FramePadding;
            ImDrawListPtr draw = ImGui.GetWindowDrawList();

            float lineHeight = ImGui.GetTextLineHeight();
            float width = avail.X - 200 - spacing.X;
            bool handled = false;

            ImGuiStylePtr style = ImGui.GetStyle();

            uint id = ImGui.GetID("Breadcrumb"u8);

            if (breadcrumbs)
            {
                Vector2 size = new(width, 0);
                Vector2 label_size = new(0, lineHeight);
                Vector2 frame_size = ImGui.CalcItemSize(size, ImGui.CalcItemWidth(), label_size.Y + style.FramePadding.Y * 2.0f); // Arbitrary default of 8 lines high for multi-line
                Vector2 total_size = new(frame_size.X + (label_size.X > 0.0f ? style.ItemInnerSpacing.X + label_size.X : 0.0f), frame_size.Y);

                ImRect frame_bb = new() { Min = pos, Max = pos + frame_size };
                ImRect total_bb = new() { Min = frame_bb.Min, Max = frame_bb.Min + total_size };
                ImGui.ItemSizeRect(total_bb, style.FramePadding.Y);
                if (!ImGui.ItemAdd(total_bb, id, &frame_bb, ImGuiItemFlags.None))
                {
                    return;
                }

                ImGui.RenderNavHighlight(frame_bb, id, ImGuiNavHighlightFlags.None);
                ImGui.RenderFrame(frame_bb.Min, frame_bb.Max, ImGui.GetColorU32(ImGuiCol.FrameBg), true, ImGui.GetStyle().FrameRounding);

                ImGui.SameLine();
                var cursorEnd = ImGui.GetCursorPos();
                ImGui.SetCursorPos(cursor);

                ImGui.PushClipRect(total_bb.Min, total_bb.Max, true);

                ReadOnlySpan<char> part = currentFolder.AsSpan();
                bool first = true;
                Span<byte> partBuffer = stackalloc byte[1024];
                int idxBase = 0;

                while (part.Length > 0)
                {
                    int index = part.IndexOf(Path.DirectorySeparatorChar);
                    if (index == -1)
                    {
                        index = part.Length;
                    }

                    idxBase += index + 1;

                    var partBase = part[..index];
                    int idx = Encoding.UTF8.GetBytes(partBase, partBuffer);
                    partBuffer[idx] = 0;

                    if (!first)
                    {
                        ImGui.SameLine();

                        ImGui.TextEx(">"u8, (byte*)null, ImGuiTextFlags.None);
                        ImGui.SameLine();
                    }

                    if (ImGuiButton.TransparentButton(partBuffer))
                    {
                        handled = true;
                        if (idxBase < currentFolder.Length)
                        {
                            CurrentFolder = currentFolder[..idxBase];
                        }
                    }
                    if (ImGui.IsItemHovered())
                    {
                        handled = true;
                    }

                    first = false;

                    if (index + 1 >= part.Length)
                    {
                        break;
                    }
                    part = part[(index + 1)..];
                }

                ImGui.PopClipRect();

                ImGui.SetCursorPos(cursorEnd);
            }

            if (!handled && ImGui.IsMouseHoveringRect(pos, pos + new Vector2(width, lineHeight)))
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    breadcrumbs = !breadcrumbs;
                }
            }

            if (!breadcrumbs)
            {
                ImGui.SetCursorPos(cursor);
                ImGui.PushItemWidth(width);
                ImGui.InputText("##Path", ref currentFolder, 1024);
                ImGui.PopItemWidth();
                if (!ImGui.IsItemFocused())
                {
                    breadcrumbs = true;
                }
                ImGui.SameLine();
                return;
            }
        }

        protected unsafe bool MainPanel(float footerHeightToReserve)
        {
            if (currentDir.Exists)
            {
                ImGuiTableFlags flags =
                    ImGuiTableFlags.Reorderable |
                    ImGuiTableFlags.Resizable |
                    ImGuiTableFlags.Hideable |
                    ImGuiTableFlags.Sortable |
                    ImGuiTableFlags.SizingFixedFit |
                    ImGuiTableFlags.ScrollX |
                    ImGuiTableFlags.ScrollY |
                    ImGuiTableFlags.PadOuterX | ImGuiTableFlags.ContextMenuInBody;
                var avail = ImGui.GetContentRegionAvail();

                bool visible = ImGui.BeginTable("0", 4, flags, new Vector2(avail.X + ImGui.GetStyle().WindowPadding.X, -footerHeightToReserve));
                if (!visible)
                {
                    ImGui.EndTable();
                    return false;
                }

                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.DefaultSort);
                ImGui.TableSetupColumn("Date Modified", ImGuiTableColumnFlags.None);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None);
                ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.None);

                ImGui.TableSetupScrollFreeze(0, 1);

                ImGui.TableHeadersRow();

                ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();

                if (!sortSpecs.IsNull)
                {
                    int sortColumnIndex = sortSpecs.Specs.ColumnIndex;
                    bool ascending = sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending;
                    IComparer<FileSystemItem> comparer;
                    if (ascending)
                    {
                        comparer = sortColumnIndex switch
                        {
                            0 => new AscendingComparer<FileSystemItem, CompareByNameComparer>(),
                            1 => new AscendingComparer<FileSystemItem, CompareByDateModifiedComparer>(),
                            2 => new AscendingComparer<FileSystemItem, CompareByTypeComparer>(),
                            3 => new AscendingComparer<FileSystemItem, CompareBySizeComparer>(),
                            _ => new AscendingComparer<FileSystemItem, CompareByNameComparer>(),
                        };
                    }
                    else
                    {
                        comparer = sortColumnIndex switch
                        {
                            0 => new CompareByNameComparer(),
                            1 => new CompareByDateModifiedComparer(),
                            2 => new CompareByTypeComparer(),
                            3 => new CompareBySizeComparer(),
                            _ => new CompareByNameComparer(),
                        };
                    }

                    entries.Sort(comparer);
                }

                bool shift = ImGui.GetIO().KeyShift;
                bool ctrl = ImGui.GetIO().KeyCtrl;

                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];

                    ImGui.TableNextRow();
                    if (ImGui.TableSetColumnIndex(0))
                    {
                        bool selected = IsSelected(entry);
                        if (entry.IsFolder)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.87f, 0.37f, 1.0f));
                        }

                        ImGui.Text(entry.Icon);
                        ImGui.SameLine();

                        if (entry.IsFolder)
                        {
                            ImGui.PopStyleColor();
                        }

                        if (ImGui.Selectable(entry.Name, selected, ImGuiSelectableFlags.NoAutoClosePopups | ImGuiSelectableFlags.SpanAllColumns))
                        {
                            OnClicked(entry, shift, ctrl);
                        }

                        if (ImGui.BeginPopupContextItem(entry.Name, ImGuiPopupFlags.MouseButtonRight))
                        {
                            ImGui.Text("Test"u8);
                            ImGui.EndPopup();
                        }

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            if (entry.IsFolder)
                            {
                                CurrentFolder = entry.Path;
                            }

                            OnDoubleClicked(entry, shift, ctrl);
                        }
                    }

                    if (ImGui.TableSetColumnIndex(1))
                    {
                        ImGui.TextDisabled($"{entry.DateModified:dd/mm/yyyy HH:mm}");
                    }

                    if (ImGui.TableSetColumnIndex(2))
                    {
                        ImGui.TextDisabled(entry.Type);
                    }

                    if (entry.IsFile && ImGui.TableSetColumnIndex(3))
                    {
                        DisplaySize(entry.Size);
                    }
                }

                ImGui.EndTable();
            }

            return false;
        }

        protected void SidePanel()
        {
            void Display(FileSystemItem item, bool first = true)
            {
                if ((item.Flags & FileSystemItemFlags.Folder) == 0)
                {
                    return;
                }

                Vector4 color = item.IsFolder && !first ? new(1.0f, 0.87f, 0.37f, 1.0f) : new(1.0f, 1.0f, 1.0f, 1.0f);
                bool isOpen = ImGuiTreeNode.IconTreeNode(item.Name, item.Icon, color, ImGuiTreeNodeFlags.OpenOnArrow);

                if (ImGui.IsItemHovered() && ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    CurrentFolder = item.Path;
                }

                if (isOpen)
                {
                    foreach (var subFolder in FileSystemHelper.GetFileSystemEntries(item.Path, RefreshFlags.Folders | RefreshFlags.IgnoreHidden, null))
                    {
                        Display(subFolder, false);
                    }

                    ImGui.TreePop();
                }
            }

            ImGui.Indent();

            ImGui.Text($"{MaterialIcons.Home}");
            ImGui.SameLine();
            if (ImGui.Selectable("Home"u8, false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.NoAutoClosePopups))
            {
                CurrentFolder = rootFolder;
            }
            ImGui.Unindent();

            ImGui.Separator();

            ImGui.Indent();
            foreach (var dir in FileSystemHelper.SpecialDirs)
            {
                ImGui.Text(dir.Icon);
                ImGui.SameLine();
                if (ImGui.Selectable(dir.Name, false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.NoAutoClosePopups))
                {
                    CurrentFolder = dir.Path;
                }
            }
            ImGui.Unindent();
            ImGui.Separator();

            ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 5f);
            if (ImGui.TreeNodeEx($"{MaterialIcons.Computer} Computer", ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.DefaultOpen))
            {
                foreach (var dir in FileSystemHelper.LogicalDrives)
                {
                    Display(dir);
                }
                ImGui.TreePop();
            }
            ImGui.PopStyleVar();
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

        private static string IconSelector(string path)
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