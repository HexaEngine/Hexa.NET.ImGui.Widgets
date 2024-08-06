using Hexa.NET.ImGui;
using System.Numerics;

namespace Hexa.NET.ImGui.Widgets
{
    public static unsafe class ImGuiSplitter
    {
        public static bool VerticalSplitter(string strId, ref float width)
        {
            return VerticalSplitter(strId, ref width, float.MinValue, float.MaxValue, 0, 2, 8, false);
        }

        public static bool VerticalSplitter(string strId, ref float width, float minWidth, float maxWidth)
        {
            return VerticalSplitter(strId, ref width, minWidth, maxWidth, 0, 2, 8, false);
        }

        public static bool VerticalSplitter(string strId, ref float width, float minWidth, float maxWidth, float height)
        {
            return VerticalSplitter(strId, ref width, minWidth, maxWidth, height, 2, 8, false);
        }

        public static bool VerticalSplitter(string strId, ref float width, float minWidth, float maxWidth, float height, bool alwaysVisible)
        {
            return VerticalSplitter(strId, ref width, minWidth, maxWidth, height, 2, 8, alwaysVisible);
        }

        public static bool VerticalSplitter(string strId, ref float width, float minWidth, float maxWidth, float height, float thickness, float tolerance)
        {
            return VerticalSplitter(strId, ref width, minWidth, maxWidth, height, thickness, tolerance, false);
        }

        public static bool VerticalSplitter(string strId, ref float width, float minWidth, float maxWidth, float height, float thickness, float tolerance, bool alwaysVisible)
        {
            var io = ImGui.GetIO();

            ImGui.SameLine();

            var origin = ImGui.GetCursorScreenPos();
            var avail = ImGui.GetContentRegionAvail();
            if (height == 0)
            {
                avail.Y = height;
            }
            else if (height < 0)
            {
                avail.Y += height;
            }

            Vector2 min = origin;
            Vector2 max = origin + new Vector2(thickness, avail.Y);
            ImRect bb = new() { Max = max, Min = min };
            ImRect bbTolerance = new() { Max = max + new Vector2(tolerance, 0), Min = min + new Vector2(-tolerance, 0) };

            uint id = ImGui.GetID(strId);

            ImGui.ItemSizeRect(bb, 0);
            if (!ImGui.ItemAdd(bb, id, &bbTolerance, ImGuiItemFlags.None))
            {
                return false;
            }

            var drawList = ImGui.GetWindowDrawList();
            bool hovered;
            bool held;
            ImGui.ButtonBehavior(bbTolerance, id, &hovered, &held, ImGuiButtonFlags.None);

            if (alwaysVisible || hovered)
            {
                drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.ButtonHovered));
                if (hovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
                }
            }

            if (held)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
                drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.ButtonActive));
                width += io.MouseDelta.X;
                if (width < minWidth) width = minWidth;
                if (width > maxWidth) width = maxWidth;
            }

            return held;
        }

        public static bool HorizontalSplitter(string strId, ref float height)
        {
            return HorizontalSplitter(strId, ref height, float.MinValue, float.MaxValue, 0, 2, 8, false);
        }

        public static bool HorizontalSplitter(string strId, ref float height, float minHeight, float maxHeight)
        {
            return HorizontalSplitter(strId, ref height, minHeight, maxHeight, 0, 2, 8, false);
        }

        public static bool HorizontalSplitter(string strId, ref float height, float minHeight, float maxHeight, float width)
        {
            return HorizontalSplitter(strId, ref height, minHeight, maxHeight, width, 2, 8, false);
        }

        public static bool HorizontalSplitter(string strId, ref float height, float minHeight, float maxHeight, float width, bool alwaysVisible)
        {
            return HorizontalSplitter(strId, ref height, minHeight, maxHeight, width, 2, 8, alwaysVisible);
        }

        public static bool HorizontalSplitter(string strId, ref float height, float minHeight, float maxHeight, float width, float thickness, float tolerance)
        {
            return HorizontalSplitter(strId, ref height, minHeight, maxHeight, width, thickness, tolerance, false);
        }

        public static bool HorizontalSplitter(string strId, ref float height, float minHeight, float maxHeight, float width, float thickness, float tolerance, bool alwaysVisible)
        {
            var io = ImGui.GetIO();

            var origin = ImGui.GetCursorScreenPos();
            var avail = ImGui.GetContentRegionAvail();
            if (width == 0)
            {
                avail.X = width;
            }
            else if (width < 0)
            {
                avail.X += width;
            }

            Vector2 min = origin;
            Vector2 max = origin + new Vector2(thickness, avail.Y);
            ImRect bb = new() { Max = max, Min = min };
            ImRect bbTolerance = new() { Max = max + new Vector2(tolerance, 0), Min = min + new Vector2(-tolerance, 0) };

            uint id = ImGui.GetID(strId);

            ImGui.ItemSizeRect(bb, 0);
            if (!ImGui.ItemAdd(bb, id, &bbTolerance, ImGuiItemFlags.None))
            {
                return false;
            }

            var drawList = ImGui.GetWindowDrawList();
            bool hovered;
            bool held;
            ImGui.ButtonBehavior(bbTolerance, id, &hovered, &held, ImGuiButtonFlags.None);

            if (alwaysVisible || hovered)
            {
                drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.ButtonHovered));
                if (hovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNs);
                }
            }

            if (held)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNs);
                drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.ButtonActive));
                height += io.MouseDelta.Y;
                if (height < minHeight) height = minHeight;
                if (height > maxHeight) height = maxHeight;
            }

            return held;
        }
    }
}