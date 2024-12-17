namespace TestApp
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.Utilities.Text;
    using System;
    using System.Globalization;
    using System.Numerics;

    public unsafe class WidgetDemo : ImWindow
    {
        public WidgetDemo()
        {
            IsEmbedded = true;
        }

        protected override string Name { get; } = "Demo";

        public override void DrawContent()
        {
            DrawBreadcrumb();
            DrawSpinner();
            DrawProgressBar();
            DrawButtons();
            DrawSplitters();
            DrawMessageBox();
            DrawDialogs();
            DrawDateTimes();
            DrawFormats();
            DrawTimeSpans();
        }

        private void DrawFormats()
        {
            if (!ImGui.CollapsingHeader("Numbers"))
            {
                return;
            }

            const int stackSize = 2048;
            byte* stack = stackalloc byte[stackSize];

            Utf8Formatter.Format(1.123f, stack, stackSize);
            ImGui.Text(stack);
            Utf8Formatter.Format(float.NaN, stack, stackSize);
            ImGui.Text(stack);
            Utf8Formatter.Format(float.PositiveInfinity, stack, stackSize);
            ImGui.Text(stack);
            Utf8Formatter.Format(float.NegativeInfinity, stack, stackSize);
            ImGui.Text(stack);

            Utf8Formatter.Format(1.123, stack, stackSize);
            ImGui.Text(stack);
            Utf8Formatter.Format(double.NaN, stack, stackSize);
            ImGui.Text(stack);
            Utf8Formatter.Format(double.PositiveInfinity, stack, stackSize);
            ImGui.Text(stack);
            Utf8Formatter.Format(double.NegativeInfinity, stack, stackSize);
            ImGui.Text(stack);

            Utf8Formatter.Format(13453400, stack, stackSize);
            ImGui.Text(stack);
        }

        private readonly DateTime start = DateTime.Now;

        private void DrawTimeSpans()
        {
            if (!ImGui.CollapsingHeader("Time Spans"))
            {
                return;
            }

            DateTime now = DateTime.Now;
            TimeSpan span = now - start;

            const int stackSize = 2048;
            byte* stack = stackalloc byte[stackSize];

            Utf8Formatter.Format(span, stack, stackSize);
            ImGui.Text(stack);

            Utf8Formatter.Format(span, stack, stackSize, "d");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "dd");
            ImGui.Text(stack);

            Utf8Formatter.Format(span, stack, stackSize, "h");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "hh");
            ImGui.Text(stack);

            Utf8Formatter.Format(span, stack, stackSize, "m");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "mm");
            ImGui.Text(stack);

            Utf8Formatter.Format(span, stack, stackSize, "s");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "ss");
            ImGui.Text(stack);

            Utf8Formatter.Format(span, stack, stackSize, "fffffff");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "ffffff");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "fffff");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "ffff");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "fff");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "ff");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "f");
            ImGui.Text(stack);

            Utf8Formatter.Format(span, stack, stackSize, "FFFFFFF");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "FFFFFF");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "FFFFF");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "FFFF");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "FFF");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "FF");
            ImGui.Text(stack);
            Utf8Formatter.Format(span, stack, stackSize, "F");
            ImGui.Text(stack);

            Utf8Formatter.Format(span, stack, stackSize, "'yMmsK'");
            ImGui.Text(stack);

            Utf8Formatter.Format(TimeSpan.FromHours(1), stack, stackSize, "h[-s]");
            ImGui.Text(stack);
        }

        private void DrawDateTimes()
        {
            if (!ImGui.CollapsingHeader("Date Times"))
            {
                return;
            }

            const int stackSize = 2048;
            byte* stack = stackalloc byte[stackSize];
            DateTime time = DateTime.Now;

            Utf8Formatter.Format(time, stack, stackSize);
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "d");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "dd");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "ddd");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "dddd");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "M");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "MM");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "MMM");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "MMMM");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "y");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "yy");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "yyy");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "yyyy");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "yyyyy");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "h");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "hh");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "t");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "tt");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "H");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "HH");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "m");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "mm");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "s");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "ss");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "fffffff");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "ffffff");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "fffff");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "ffff");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "fff");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "ff");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "f");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "FFFFFFF");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "FFFFFF");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "FFFFF");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "FFFF");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "FFF");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "FF");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "F");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "g");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "z");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "zz");
            ImGui.Text(stack);
            Utf8Formatter.Format(time, stack, stackSize, "zzz");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "K");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "'yMmsK'");
            ImGui.Text(stack);

            Utf8Formatter.Format(time, stack, stackSize, "\"yMmsK\"");
            ImGui.Text(stack);

            foreach (var pattern in CultureInfo.CurrentCulture.DateTimeFormat.GetAllDateTimePatterns())
            {
                int written = Utf8Formatter.Format(time, stack, stackSize, pattern);
                ImGui.Text(stack);
            }

            foreach (var pattern in CultureInfo.InvariantCulture.DateTimeFormat.GetAllDateTimePatterns())
            {
                int written = Utf8Formatter.Format(time, stack, stackSize, pattern, CultureInfo.InvariantCulture);
                ImGui.Text(stack);
            }
            var utc = DateTime.UtcNow;
            var jp = CultureInfo.GetCultureInfo("ja-JP");
            foreach (var pattern in jp.DateTimeFormat.GetAllDateTimePatterns())
            {
                int written = Utf8Formatter.Format(utc, stack, stackSize, pattern, jp);
                ImGui.Text(stack);
            }
        }

        private string path3 = "C:\\users\\user\\Desktop\\very\\long\\long\\long\\path";

        private void DrawBreadcrumb()
        {
            if (ImGui.CollapsingHeader("Breadcrumbs"))
            {
                ImGuiBreadcrumb.Breadcrumb("##Breadcrumb3", ref path3);
            }
        }

        private static void DrawDialogs()
        {
            if (!ImGui.CollapsingHeader("Dialogs"))
            {
                return;
            }

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

        private MessageBoxType messageBoxType;
        private string messageBoxText = "Message Box Text";
        private string messageBoxTitle = "Message Box Title";

        private void DrawMessageBox()
        {
            if (!ImGui.CollapsingHeader("Message Boxes"))
            {
                return;
            }

            ComboEnumHelper<MessageBoxType>.Combo("Type", ref messageBoxType);
            ImGui.InputText("Title", ref messageBoxTitle, 100);
            ImGui.InputTextMultiline("Text", ref messageBoxText, 1000, new Vector2(400, 200));
            if (ImGui.Button("Show"))
            {
                MessageBox.Show(messageBoxTitle, messageBoxText, messageBoxType);
            }
        }

        private float splitterHPosition = 200;
        private float splitterVPosition = 200;

        private void DrawSplitters()
        {
            if (!ImGui.CollapsingHeader("Splitters"))
            {
                return;
            }

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

        private bool buttonToggled = false;
        private bool switchToggled = false;

        private void DrawButtons()
        {
            if (!ImGui.CollapsingHeader("Buttons"))
            {
                return;
            }

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

        private Vector4 progressBarColor = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered);
        private AnimationType progressBarAnimationType = AnimationType.EaseOutCubic;

        private void DrawProgressBar()
        {
            if (!ImGui.CollapsingHeader("Progress Bar"))
            {
                return;
            }

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
            ImGuiProgressBar.ProgressBar(value, new(400, 20), ImGui.GetColorU32(ImGuiCol.Button), ImGui.ColorConvertFloat4ToU32(progressBarColor));
        }

        private Vector4 spinnerColor = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered);
        private float spinnerRadius = 16;
        private float spinnerThickness = 5f;

        private void DrawSpinner()
        {
            if (!ImGui.CollapsingHeader("Spinner"))
            {
                return;
            }

            ImGui.ColorEdit4("Color", ref spinnerColor);
            ImGui.DragFloat("Radius", ref spinnerRadius, 0.1f, 1, 100);
            ImGui.DragFloat("Thickness", ref spinnerThickness, 0.1f, 1, 100);
            ImGuiSpinner.Spinner(spinnerRadius, spinnerThickness, ImGui.ColorConvertFloat4ToU32(spinnerColor));
        }
    }
}