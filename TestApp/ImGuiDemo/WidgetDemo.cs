namespace TestApp.ImGuiDemo
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using System.Numerics;

    public unsafe class WidgetDemo : ImWindow
    {
        public WidgetDemo()
        {
            IsEmbedded = true;
        }

        protected override string Name { get; } = "Demo";

        public override void DrawWindow(ImGuiWindowFlags overwriteFlags)
        {
            base.DrawWindow(overwriteFlags);
        }

        public override void DrawContent()
        {
            DrawBreadcrumb();
            DrawSpinner();
            DrawProgressBar();
            DrawButtons();
            DrawSplitters();
            DrawMessageBox();
            DrawDialogs();
        }

        private string path1 = "/home/user/Desktop";
        private string path2 = "C:\\users\\user\\Desktop\\";
        private string path3 = "C:\\users\\user\\Desktop\\very\\long\\long\\long\\path";

        private void DrawBreadcrumb()
        {
            if (ImGui.CollapsingHeader("Breadcrumbs"))
            {
                //ImGuiBreadcrumb.Breadcrumb("##Breadcrumb1", ref path1);
                //ImGui.SameLine();
                //ImGui.Text("Hello world");
                //ImGuiBreadcrumb.Breadcrumb("##Breadcrumb2", ref path2);
                ImGuiBreadcrumb.Breadcrumb("##Breadcrumb3", ref path3);
            }
        }

        private static void DrawDialogs()
        {
            if (ImGui.CollapsingHeader("Dialogs"))
            {
                ImGui.Text("Material Icon Font has to be loaded!");
                if (ImGui.Button("Open File Dialog"))
                {
                    OpenFileDialog openFileDialog = new();
                    openFileDialog.Show();
                }
                if (ImGui.Button("Open File Dialog (Multiselect)"))
                {
                    OpenFileDialog openFileDialog = new();
                    openFileDialog.AllowMultipleSelection = true;
                    openFileDialog.Show();
                }
                if (ImGui.Button("Open File Dialog (Filtered)"))
                {
                    OpenFileDialog openFileDialog = new();
                    openFileDialog.AllowedExtensions.Add(".txt");
                    openFileDialog.OnlyAllowFilteredExtensions = true;
                    openFileDialog.Show();
                }
                if (ImGui.Button("Open File Dialog (Folders Only)"))
                {
                    OpenFileDialog openFileDialog = new();
                    openFileDialog.OnlyAllowFolders = true;
                    openFileDialog.Show();
                }
                if (ImGui.Button("Save File Dialog"))
                {
                    SaveFileDialog saveFileDialog = new();
                    saveFileDialog.Show();
                }
            }
        }

        private MessageBoxType messageBoxType;
        private string messageBoxText = "Message Box Text";
        private string messageBoxTitle = "Message Box Title";

        private void DrawMessageBox()
        {
            if (ImGui.CollapsingHeader("Message Boxes"))
            {
                ComboEnumHelper<MessageBoxType>.Combo("Type", ref messageBoxType);
                ImGui.InputText("Title", ref messageBoxTitle, 100);
                ImGui.InputTextMultiline("Text", ref messageBoxText, 1000, new Vector2(400, 200));
                if (ImGui.Button("Show"))
                {
                    MessageBox.Show(messageBoxTitle, messageBoxText, messageBoxType);
                }
            }
        }

        private float splitterHPosition = 200;
        private float splitterVPosition = 200;

        private void DrawSplitters()
        {
            if (ImGui.CollapsingHeader("Splitters"))
            {
                ImGui.BeginChild("C1", new Vector2(splitterVPosition, -splitterHPosition));
                ImGui.Text("Child 1");
                ImGui.EndChild();

                ImGuiSplitter.VerticalSplitter("Vertical Splitter", ref splitterVPosition, 0, float.MaxValue, -splitterHPosition, 4, 8);

                ImGui.BeginChild("C2", new Vector2(0, -splitterHPosition));
                ImGui.Text("Child 2");
                ImGui.EndChild();

                ImGuiSplitter.HorizontalSplitter("Horizontal Splitter", ref splitterHPosition, 0, float.MaxValue, 0, 4, 8);

                ImGui.BeginChild("C3");
                ImGui.Text("Child 3");
                ImGui.EndChild();
            }
        }

        private bool buttonToggled = false;
        private bool switchToggled = false;

        private void DrawButtons()
        {
            if (ImGui.CollapsingHeader("Buttons"))
            {
                if (ImGuiButton.TransparentButton("Transparent Button"))
                {
                }
                ImGui.Separator();
                if (ImGuiButton.ToggleButton("Toggle Button", ref buttonToggled))
                {
                }
                ImGui.Separator();
                if (ImGuiButton.ToggleSwitch("Switch", ref switchToggled))
                {
                }
            }
        }

        private Vector4 progressBarColor = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered);
        private AnimationType progressBarAnimationType = AnimationType.EaseOutCubic;

        private void DrawProgressBar()
        {
            if (ImGui.CollapsingHeader("Progress Bar"))
            {
                uint id = ImGui.GetID("##Progress Bar");

                ImGui.ColorEdit4("Color##ProgressBar", ref progressBarColor);
                if (ComboEnumHelper<AnimationType>.Combo("Animation Type", ref progressBarAnimationType))
                {
                    AnimationManager.StopAnimation(id);
                }

                float value = AnimationManager.GetAnimationValue(id);
                if (value == -1)
                {
                    AnimationManager.AddAnimation(id, 3, 1, progressBarAnimationType);
                }
                ImGuiProgressBar.ProgressBar("Progress Bar", value, new(400, 20), ImGui.GetColorU32(ImGuiCol.Button), ImGui.ColorConvertFloat4ToU32(progressBarColor));
            }
        }

        private Vector4 spinnerColor = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered);
        private float spinnerRadius = 16;
        private float spinnerThickness = 5f;

        private void DrawSpinner()
        {
            if (ImGui.CollapsingHeader("Spinner"))
            {
                ImGui.ColorEdit4("Color", ref spinnerColor);
                ImGui.DragFloat("Radius", ref spinnerRadius, 0.1f, 1, 100);
                ImGui.DragFloat("Thickness", ref spinnerThickness, 0.1f, 1, 100);
                ImGuiSpinner.Spinner("Spinner", spinnerRadius, spinnerThickness, ImGui.ColorConvertFloat4ToU32(spinnerColor));
            }
        }
    }
}