namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets.IO;

    public class RenameFileDialog
    {
        private bool shown;
        private string file = string.Empty;
        private string filename = string.Empty;
        private DialogResult renameFileResult;
        private bool overwrite;

        public RenameFileDialog()
        {
        }

        public RenameFileDialog(string file)
        {
            File = file;
        }

        public RenameFileDialog(bool overwrite)
        {
            Overwrite = overwrite;
        }

        public RenameFileDialog(string file, bool overwrite)
        {
            File = file;
            Overwrite = overwrite;
        }

        public bool Shown => shown;

        public string File
        {
            get => file; set
            {
                if (!System.IO.File.Exists(value))
                    return;
                file = value;
                filename = Path.GetFileName(file);
            }
        }

        public bool Overwrite { get => overwrite; set => overwrite = value; }

        public DialogResult Result => renameFileResult;

        public void Show()
        {
            shown = true;
        }

        public void Hide()
        {
            shown = false;
        }

        public bool Draw()
        {
            if (!shown) return false;
            bool result = false;
            if (ImGui.Begin("Rename file", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.SetWindowFocus();

                ImGui.InputText("New name", ref filename, 2048);

                if (ImGui.Button("Cancel"))
                {
                    renameFileResult = DialogResult.Cancel;
                }
                ImGui.SameLine();
                if (ImGui.Button("Ok"))
                {
                    string dir = FileUtils.GetDirectoryName(file.AsSpan()).ToString();
                    string newPath = Path.Combine(dir, filename);
#if NET5_0_OR_GREATER
                    System.IO.File.Move(file, newPath, overwrite);
#else
                    if (System.IO.File.Exists(newPath))
                    {
                        System.IO.File.Delete(newPath);
                    }
                    System.IO.File.Move(file, newPath);
#endif
                    renameFileResult = DialogResult.Ok;
                    result = true;
                }
                ImGui.End();
            }

            if (result)
            {
                shown = false;
            }

            return result;
        }
    }
}