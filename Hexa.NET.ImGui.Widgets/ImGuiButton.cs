namespace Hexa.NET.ImGui.Widgets
{
    using Hexa.NET.ImGui;
    using System;
    using System.Numerics;

    public unsafe class ImGuiButton
    {
        public static bool ToggleButton(string label, bool selected = false)
        {
            uint id = ImGui.GetID(label);

            ImGuiStylePtr style = ImGui.GetStyle();

            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 size = ImGui.CalcTextSize(label);
            Vector2 padding = style.FramePadding - new Vector2(style.FrameBorderSize * 2);
            ImRect bb = new() { Min = pos + new Vector2(padding.X, 0), Max = new(pos.X + size.X, pos.Y + size.Y) };
            ImRect bbFull = new(pos, new Vector2(pos.X + size.X, pos.Y + size.Y) + padding * 2);

            ImGui.ItemSizeRect(bbFull, 0.0f);
            if (!ImGui.ItemAdd(bbFull, id, &bbFull, ImGuiItemFlags.None))
                return false;

            uint textColor = ImGui.GetColorU32(ImGuiCol.Text);
            uint hoverColor = ImGui.GetColorU32(ImGuiCol.ButtonHovered);
            uint activeColor = ImGui.GetColorU32(ImGuiCol.ButtonActive);
            uint selectedColor = ImGui.GetColorU32(ImGuiCol.TabSelectedOverline);
            uint selectedBgColor = ImGui.GetColorU32(ImGuiCol.TabSelected);

            ImDrawList* draw = ImGui.GetWindowDrawList();

            bool isHovered = false;
            bool isClicked = ImGui.ButtonBehavior(bbFull, id, &isHovered, null, 0);
            bool isActive = isHovered && ImGui.IsMouseDown(0);

            uint color = isActive ? activeColor : isHovered ? hoverColor : selected ? selectedBgColor : default;

            if (isActive || isHovered || selected)
            {
                draw->AddRectFilled(bbFull.Min, bbFull.Max, color, style.FrameRounding);
            }

            if (selected)
            {
                draw->AddRect(bbFull.Min, bbFull.Max, selectedColor, style.FrameRounding, 2);
            }

            draw->AddText(bb.Min, textColor, label);

            return isClicked;
        }

        public static bool TransparentButton(ReadOnlySpan<byte> label)
        {
            fixed (byte* ptr = label)
            {
                return TransparentButton(ptr, default, ImGuiButtonFlags.None);
            }
        }

        public static bool TransparentButton(string label)
        {
            int sizeInBytes = System.Text.Encoding.UTF8.GetByteCount(label);
            byte* pLabel;
            if (sizeInBytes + 1 >= 2048)
            {
                pLabel = AllocT<byte>(sizeInBytes + 1);
            }
            else
            {
                byte* stackLabel = stackalloc byte[sizeInBytes + 1];
                pLabel = stackLabel;
            }
            System.Text.Encoding.UTF8.GetBytes(label, new Span<byte>(pLabel, sizeInBytes));
            pLabel[sizeInBytes] = 0;
            bool result = TransparentButton(pLabel, default, ImGuiButtonFlags.None);
            if (sizeInBytes + 1 >= 2048)
            {
                Free(pLabel);
            }
            return result;
        }

        public static bool TransparentButton(byte* label, Vector2 sizeArg, ImGuiButtonFlags flags)
        {
            ImGuiWindow* window = ImGui.GetCurrentWindow();
            if (window->SkipItems != 0)
                return false;

            uint id = ImGui.GetID(label);

            ImGuiStylePtr style = ImGui.GetStyle();

            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 labelSize = ImGui.CalcTextSize(label, (byte*)null, true);
            if ((flags & (ImGuiButtonFlags)ImGuiButtonFlagsPrivate.AlignTextBaseLine) != 0 && style.FramePadding.Y < window->DC.CurrLineTextBaseOffset) // Try to vertically align buttons that are smaller/have no padding so that text baseline matches (bit hacky, since it shouldn't be a flag)
                pos.Y += window->DC.CurrLineTextBaseOffset - style.FramePadding.Y;
            Vector2 size = ImGui.CalcItemSize(sizeArg, labelSize.X + style.FramePadding.X * 2.0f, labelSize.Y + style.FramePadding.Y * 2.0f);

            ImRect bb = new() { Min = pos, Max = pos + size };
            ImGui.ItemSizeVec2(size, style.FramePadding.Y);
            if (!ImGui.ItemAdd(bb, id, &bb, ImGuiItemFlags.None))
                return false;

            uint hoverColor = ImGui.GetColorU32(ImGuiCol.ButtonHovered);
            uint activeColor = ImGui.GetColorU32(ImGuiCol.ButtonActive);

            ImDrawList* draw = ImGui.GetWindowDrawList();

            bool hovered, held;
            bool pressed = ImGui.ButtonBehavior(bb, id, &hovered, &held, flags);

            ImGui.RenderNavHighlight(bb, id, default);
            if (pressed || hovered || held)
            {
                uint col = ImGui.GetColorU32(held && hovered ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button);
                ImGui.RenderFrame(bb.Min, bb.Max, col, true, style.FrameRounding);
            }

            ImGui.RenderTextClipped(bb.Min + style.FramePadding, bb.Max - style.FramePadding, label, (byte*)null, &labelSize, style.ButtonTextAlign, &bb);

            return pressed;
        }
    }
}