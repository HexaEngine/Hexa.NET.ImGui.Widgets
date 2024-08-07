namespace Hexa.NET.ImGui.Widgets
{
    using Hexa.NET.ImGui;
    using System;
    using System.Numerics;

    public static class ImGuiSpinner
    {
        public static unsafe void Spinner(string label, float radius, float thickness, uint color)
        {
            ImGuiWindow* window = ImGui.GetCurrentWindow();
            if (window->SkipItems == 1)
            {
                return;
            }

            ImDrawList* drawList = ImGui.GetWindowDrawList();
            var g = ImGui.GetCurrentContext();
            var style = ImGui.GetStyle();
            uint id = ImGui.GetID(label);

            var pos = ImGui.GetCursorScreenPos();

            Vector2 size = new(radius * 2, (radius + style.FramePadding.Y) * 2);

            ImRect bb = new(pos, pos + size);

            ImGui.ItemSizeRect(bb, -1);
            if (!ImGui.ItemAdd(bb, id, null, ImGuiItemFlags.None))
            {
                return;
            }

            // Render
            ImGui.PathClear(drawList);

            const int num_segments = 24;

            int start = (int)Math.Abs(MathF.Sin((float)(g.Time * 1.8f)) * (num_segments - 5));

            float a_min = float.Pi * 2.0f * start / num_segments;
            float a_max = float.Pi * 2.0f * ((float)num_segments - 3) / num_segments;

            Vector2 center = pos + new Vector2(radius, radius + style.FramePadding.Y);

            for (var i = 0; i < num_segments; i++)
            {
                float a = a_min + i / (float)num_segments * (a_max - a_min);
                var time = (float)g.Time;
                var pp = new Vector2(center.X + MathF.Cos(a + time * 8) * radius, center.Y + MathF.Sin(a + time * 8) * radius);
                drawList->PathLineTo(pp);
            }

            ImGui.PathStroke(drawList, color, 0, thickness);
        }
    }
}