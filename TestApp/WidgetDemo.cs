using System.Numerics;

namespace TestApp
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.ImGui.Widgets.Extensions;
    using Hexa.NET.ImGui.Widgets.ImCurveEdit;
    using Hexa.NET.ImGui.Widgets.ImSequencer;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using Hexa.NET.Utilities.Text;
    using System;
    using System.Globalization;
    using System.Numerics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using CurveType = Hexa.NET.ImGui.Widgets.ImCurveEdit.CurveType;

    public unsafe class WidgetDemo2 : ImWindow
    {
        public override string Name { get; } = "Demo2";

        public override void DrawContent()
        {
        }

        public override void Dispose()
        {
        }
    }

    public unsafe class RampEdit : CurveContext
    {
        public RampEdit()
        {
            mPts[0][0] = new Vector2(0, 0);
            mPts[0][1] = new Vector2(0.25f, 0.25f);
            mPointCount[0] = 2;

            mbVisible[0] = true;
            Max = new Vector2(1.0f, 1.0f);
            Min = new Vector2(0.0f, 0.0f);
        }

        public override int GetCurveCount()
        {
            return 1;
        }

        public override bool IsVisible(int curveIndex)
        {
            return mbVisible[curveIndex];
        }

        public override int GetPointCount(int curveIndex)
        {
            return mPointCount[curveIndex];
        }

        public override uint GetCurveColor(int curveIndex)
        {
            uint[] cols = { 0xFF0000FF, 0xFF00FF00, 0xFFFF0000 };
            return cols[curveIndex];
        }

        public override Span<Vector2> GetPoints(int curveIndex)
        {
            return mPts[curveIndex];
        }

        public override CurveType GetCurveType(int curveIndex)
        {
            return CurveType.CurveSmooth;
        }

        private int InsertPoint(int curveIndex, Vector2 value)
        {
            var points = mPts[curveIndex];
            var count = mPointCount[curveIndex]++;

            int insertIndex = Array.BinarySearch(points, 0, count, value, Comparer.Instance);
            if (insertIndex < 0) insertIndex = ~insertIndex;

            Array.Copy(points, insertIndex, points, insertIndex + 1, count - insertIndex);

            points[insertIndex] = value;
            return insertIndex;
        }

        public override int EditPoint(int curveIndex, int pointIndex, Vector2 value)
        {
            var points = mPts[curveIndex];
            var count = mPointCount[curveIndex];

            bool canStay = (pointIndex == 0 || value.X >= points[pointIndex - 1].X) &&
                (pointIndex == count - 1 || value.X <= points[pointIndex + 1].X);

            if (canStay)
            {
                points[pointIndex] = value;
                return pointIndex;
            }

            int insertIndex = Array.BinarySearch(points, 0, mPointCount[curveIndex], value, Comparer.Instance);
            if (insertIndex < 0) insertIndex = ~insertIndex;

            if (insertIndex < pointIndex)
            {
                Array.Copy(points, insertIndex, points, insertIndex + 1, pointIndex - insertIndex);
            }
            else
            {
                Array.Copy(points, pointIndex + 1, points, pointIndex, insertIndex - pointIndex - 1);
                insertIndex--;
            }

            points[insertIndex] = value;
            return insertIndex;
        }

        public override void AddPoint(int curveIndex, Vector2 value)
        {
            if (mPointCount[curveIndex] >= 8)
                return;
            InsertPoint(curveIndex, value);
        }

        public override uint GetBackgroundColor()
        { return 0; }

        public Vector2[][] mPts = [new Vector2[8]];
        public int[] mPointCount = new int[3];
        public bool[] mbVisible = new bool[3];

        private class Comparer : IComparer<Vector2>
        {
            public static readonly Comparer Instance = new();

            public int Compare(Vector2 a, Vector2 b)
            {
                return a.X.CompareTo(b.X);
            }
        }

        private void SortValues(int curveIndex)
        {
            Array.Sort(mPts[curveIndex], Comparer.Instance);
        }
    };

    public unsafe class MySequence : SequenceInterface
    {
        public struct Item
        {
            public int Start;
            public int End;
            public int Type;
            public bool Expanded;

            public Item(int type, int start, int end, bool expanded)
            {
                Start = start;
                End = end;
                Type = type;
                Expanded = expanded;
            }
        }

        RampEdit rampEdit = new();

        public UnsafeList<Item> items = [];

        public override int GetFrameMin() => frameMin;

        public override int GetFrameMax() => frameMax;

        public override int GetItemCount() => items.Count;

        public override int GetItemTypeCount() => 5;

        public override ReadOnlySpan<byte> GetItemTypeName(int typeIndex)
        {
            return typeIndex switch
            {
                0 => "Camera"u8,
                1 => "Music"u8,
                2 => "ScreenEffect"u8,
                3 => "FadeIn"u8,
                4 => "Animation"u8,
                _ => base.GetItemTypeName(typeIndex),
            };
        }

        public byte* Buffer = (byte*)Utils.Alloc(4096);

        public override ReadOnlySpan<byte> GetItemLabel(int index)
        {
            StrBuilder builder = new(Buffer, 4096);
            builder.Reset();
            builder.Append("[");
            builder.Append(index);
            builder.Append("] ");
            builder.Append(GetItemTypeName(items[index].Type));
            builder.End();
            return new(builder.Buffer, builder.Index);
        }

        public override nuint GetCustomHeight(int index)
        {
            return items[index].Expanded ? 300u : 0;
        }

        public override void Get(int index, int** start, int** end, int* type, uint* color)
        {
            if (start != null) *start = &items.Data[index].Start;
            if (end != null) *end = &items.Data[index].End;
            if (type != null) *type = items[index].Type;
            if (color != null) *color = 0xFFAA8080;
        }

        public override void Add(int type)
        {
            items.Add(new Item { Start = 0, End = 10, Type = type });
        }

        public override void Del(int index)
        {
            if (index >= 0 && index < items.Count)
                items.RemoveAt(index);
        }

        private int lastExpanded = -1;

        public override void DoubleClick(int index)
        {
            var state = items.Data[index].Expanded = !items.Data[index].Expanded;
            if (lastExpanded != -1)
            {
                items.Data[lastExpanded].Expanded = false;
                lastExpanded = state ? index : -1;
            }
        }

        static string[] labels = { "Translation", "Rotation", "Scale" };
        internal int frameMin;
        internal int frameMax;

        public override void CustomDraw(int index, ImDrawList* draw, ref ImRect rc, ref ImRect legendRect, ref ImRect clippingRect, ref ImRect legendClippingRect)
        {
            rampEdit.Max = new Vector2((float)(frameMax), 1.0f);
            rampEdit.Min = new Vector2((float)(frameMin), 0.0f);
            draw->PushClipRect(legendClippingRect.Min, legendClippingRect.Max, true);
            for (int i = 0; i < 3; i++)
            {
                Vector2 pta = new(legendRect.Min.X + 30, legendRect.Min.Y + i * 14.0f);
                Vector2 ptb = new(legendRect.Max.X, legendRect.Min.Y + (i + 1) * 14.0f);
                draw->AddText(pta, rampEdit.mbVisible[i] ? 0xFFFFFFFF : 0x80FFFFFF, labels[i]);
                if (new ImRect(pta, ptb).Contains(ImGui.GetMousePos()) && ImGui.IsMouseClicked(0))
                    rampEdit.mbVisible[i] = !rampEdit.mbVisible[i];
            }

            draw->PopClipRect();

            ImGui.SetCursorScreenPos(rc.Min);
            draw->PushClipRect(clippingRect.Min, clippingRect.Max, true);
            ImCurveEdit.Edit(rampEdit, rc.Max - rc.Min, (uint)(137 + index));
            draw->PopClipRect();
        }

        public override unsafe void CustomDrawCompact(int index, ImDrawList* draw_list, ref ImRect rc, ref ImRect clippingRect)
        {
            rampEdit.Max = new Vector2(frameMax, 1.0f);
            rampEdit.Min = new Vector2(frameMin, 0.0f);
            draw_list->PushClipRect(clippingRect.Min, clippingRect.Max, true);
            for (int i = 0; i < 3; i++)
            {
                for (uint j = 0; j < rampEdit.mPointCount[i]; j++)
                {
                    float p = rampEdit.mPts[i][j].X;
                    if (p < items[index].Start || p > items[index].End)
                        continue;

                    float r = (p - frameMin) / (float)(frameMax - frameMin);
                    float x = MathUtil.Lerp(rc.Min.X, rc.Max.X, r);
                    draw_list->AddLine(new Vector2(x, rc.Min.Y + 6), new Vector2(x, rc.Max.Y - 4), 0xAA000000, 4.0f);
                }
            }
            draw_list->PopClipRect();
        }
    }

    public unsafe class WidgetDemo : ImWindow
    {
        public WidgetDemo()
        {
            sequence.frameMin = -100;
            sequence.frameMax = 1000;
            sequence.items.Add(new MySequence.Item(0, 10, 30, false));
            sequence.items.Add(new MySequence.Item(1, 20, 30, false));
            sequence.items.Add(new MySequence.Item(3, 12, 60, false));
            sequence.items.Add(new MySequence.Item(2, 61, 90, false));
            sequence.items.Add(new MySequence.Item(4, 90, 99, false));
        }

        public override string Name { get; } = "Demo";

        private MySequence sequence = new();
        int currentFrame = 100;
        bool expanded = true;
        int selectedEntry = -1;
        int firstFrame = 0;

        RampEdit edit = new();

        public override void DrawContent()
        {
            ImCurveEdit.Edit(edit, ImGui.GetContentRegionAvail(), 10312);
            //ImSequencer.Sequencer(sequence, ref currentFrame, ref expanded, ref selectedEntry, ref firstFrame, SequencerOptions.EditAll);
            /*
             DrawBreadcrumb();
             DrawSpinner();
             DrawProgressBar();
             DrawButtons();
             DrawSplitters();
             DrawMessageBox();
             DrawDialogs();
             DrawDateTimes();
             DrawFormats();
             DrawTimeSpans();*/
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
            if (ImGui.Button("Open Folder Dialog"))
            {
                OpenFolderDialog openFolderDialog = new();
                openFolderDialog.Show();
            }
            if (ImGui.Button("Save File Dialog"))
            {
                SaveFileDialog saveFileDialog = new();
                saveFileDialog.Show();
            }
            if (ImGui.Button("Rename file"))
            {
                RenameDialog renameDialog = new("test.txt", RenameDialogFlags.Default | RenameDialogFlags.NoAutomaticMove);
                renameDialog.Show();
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