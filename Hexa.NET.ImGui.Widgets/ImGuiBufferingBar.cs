namespace Hexa.NET.ImGui.Widgets
{
    using Hexa.NET.ImGui;
    using System.Numerics;

    public static class ImGuiBufferingBar
    {
        public static unsafe void BufferingBar(string label, float value, Vector2 size, uint backgroundColor, uint foregroundColor)
        {
            ImGuiWindow* window = ImGui.GetCurrentWindow();
            if (window->SkipItems == 1)
            {
                return;
            }

            ImDrawList* drawList = ImGui.GetWindowDrawList();
            ImGuiContextPtr g = ImGui.GetCurrentContext();
            ImGuiStylePtr style = ImGui.GetStyle();
            uint id = ImGui.GetID(label);

            var cursorPos = ImGui.GetCursorPos();

            Vector2 pos = window->DC.CursorPos;
            size.X -= style.FramePadding.X * 2;

            cursorPos -= window->WindowPadding;

            ImRect bb = new() { Min = pos + cursorPos, Max = pos + cursorPos + size };
            ImGui.ItemSizeRect(bb, style.FramePadding.Y);
            if (!ImGui.ItemAdd(bb, id, null, ImGuiItemFlags.None))
            {
                return;
            }

            // Render
            float circleStart = size.X * 0.7f;
            float circleEnd = size.Y;
            float circleWidth = circleEnd - circleStart;

            drawList->AddRectFilled(bb.Min, new Vector2(pos.X + circleStart, bb.Max.Y), backgroundColor);
            drawList->AddRectFilled(bb.Min, new Vector2(pos.X + circleStart * value, bb.Max.Y), foregroundColor);

            float t = (float)g.Time;
            float r = size.Y / 2;
            float speed = 1.5f;

            float a = speed * 0;
            float b = speed * 0.333f;
            float c = speed * 0.666f;

            float o1 = (circleWidth + r) * (t + a - speed * (int)((t + a) / speed)) / speed;
            float o2 = (circleWidth + r) * (t + b - speed * (int)((t + b) / speed)) / speed;
            float o3 = (circleWidth + r) * (t + c - speed * (int)((t + c) / speed)) / speed;

            drawList->AddCircleFilled(new Vector2(pos.X + circleEnd - o1, bb.Min.Y + r), r, backgroundColor);
            drawList->AddCircleFilled(new Vector2(pos.X + circleEnd - o2, bb.Min.Y + r), r, backgroundColor);
            drawList->AddCircleFilled(new Vector2(pos.X + circleEnd - o3, bb.Min.Y + r), r, backgroundColor);
        }
    }
}