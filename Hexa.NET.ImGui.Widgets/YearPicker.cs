
namespace Hexa.NET.ImGui.Widgets;

using Hexa.NET.Utilities.Text;
using System.Numerics;
using System.Runtime.InteropServices;

public unsafe static class YearPicker
{
    public static bool Draw(ReadOnlySpan<byte> label, ref DateTime time)
    {
        fixed (byte* ptr = label)
        {
            return Draw(ptr, ref time);
        }
    }

    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 4)]
    private readonly struct YearPickerState
    {
        // DO NOT CHANGE THE SIZE OF THIS STRUCT WITHOUT CHANGING THE YEARPICKER METHOD,
        // IT RELIES ON BEING EXACTLY 4 BYTES TO WORK PROPERLY.
        [FieldOffset(0)]
        private readonly short editYear;
        [FieldOffset(2)]
        private readonly short userYear;

        [FieldOffset(0)]
        private readonly uint value;

        public YearPickerState(uint value)
        {
            this.value = value;
        }

        public YearPickerState(short editYear, short userYear)
        {
            this.editYear = editYear;
            this.userYear = userYear;
        }

        public YearPickerState(DateTime editState, DateTime userState)
        {
            editYear = (short)editState.Year;
            userYear = (short)userState.Year;
        }

        public YearPickerState(DateTime time) : this(time, time) { }

        public readonly uint Value => value;

        public readonly int EditYear => editYear;

        public readonly int UserYear => userYear;

        public YearPickerState AddEditYears(short years)
        {
            var newYear = editYear + years;
            newYear = ImMath.Clamp(newYear, 1, (short)DateTime.MaxValue.Year);
            return new((short)newYear, userYear);
        }

        public YearPickerState SubtractEditYears(short years)
        {
            var newYear = editYear - years;
            newYear = ImMath.Clamp(newYear, 1, (short)DateTime.MaxValue.Year);
            return new((short)newYear, userYear);
        }
    }

    public static bool Draw(byte* label, ref DateTime time)
    {
        var window = ImGuiP.GetCurrentWindow();
        if (window.SkipItems) return false;
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();

        var buttonSize = ImGuiButton.CalcInlineButtonSize(">"u8);
        var sizeYear = ImGui.CalcTextSize("XXXX"u8);
        var sizeHeader = Vector2.Max(ImGui.CalcTextSize("XXXX - XXXX"u8), new Vector2(buttonSize.X * 2, buttonSize.Y));
        var sizeCard = sizeYear + style.ItemSpacing * 2;
        const int rows = 4;
        const int cols = 4;
        var sizeCards = sizeCard * new Vector2(cols, rows);

        var size = new Vector2(Math.Max(sizeHeader.X, sizeCards.X), sizeHeader.Y + sizeCards.Y) + style.FramePadding * 2;

        var id = ImGui.GetID(label);

        ImRect bb = new(pos, pos + size);
        ImGuiP.ItemSize(bb);
        if (!ImGuiP.ItemAdd(bb, id, &bb, (ImGuiItemFlags)ImGuiItemFlagsPrivate.AllowOverlap))
        {
            return false;
        }
        var hovered = ImGuiP.ItemHoverable(bb, id, (ImGuiItemFlags)ImGuiItemFlagsPrivate.AllowOverlap);

        using ImGuiIDScope idScope = new(label);

        // Looks weird but it's safe, because YearPickerState is internally just a uint.
        ref var state = ref *(YearPickerState*)ImGui.GetStateStorage().GetIntRef(id, 0);
        if (time.Year != state.UserYear)
        {
            state = new(time, time);
        }

        pos += style.FramePadding;
        bb.Min = pos;
        bb.Max -= style.FramePadding;
        var draw = ImGui.GetWindowDrawList();
        var col = ImGui.GetColorU32(ImGuiCol.Text);
        var winBg = ColorHelper.GetCurrentWindowBg();
        col = ColorHelper.FixContrastABGR(winBg, col);

        const int yearsPerPage = cols * rows;
        var pageOffset = (int)Math.Floor((state.EditYear - 1) / (float)yearsPerPage);
        var pageStart = pageOffset * yearsPerPage;
        var builder = TextHelper.Builder;

        var page = pageStart + 1;
        FormatYear(ref builder, page);
        builder.Append(" - "u8);
        FormatYear(ref builder, pageStart + yearsPerPage);
        builder.End();

        sizeHeader = ImGui.CalcTextSize(builder);

        var innerSize = bb.Max - bb.Min;
        Vector2 headerTextPos = new(pos.X + (innerSize.X - sizeHeader.X) * 0.5f, pos.Y);

        draw.AddText(headerTextPos, col, builder);

        if (ImGuiButton.InlineButton("<"u8, bb, new()))
        {
            if (state.EditYear - yearsPerPage > 0)
            {
                state = state.SubtractEditYears(yearsPerPage);
            }
        }
        if (ImGuiButton.InlineButton(">"u8, bb, new(1, 0)))
        {
            if (state.EditYear + yearsPerPage <= DateTime.MaxValue.Year)
            {
                state = state.AddEditYears(yearsPerPage);
            }
        }

        pos.Y += Math.Max(sizeHeader.Y, buttonSize.Y) + style.ItemInnerSpacing.Y;
        var mousePos = ImGui.GetMousePos();
        var mouseDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var mouseClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        var colBtn = ImGui.GetColorU32(ImGuiCol.Button);
        var colBtnHover = ImGui.GetColorU32(ImGuiCol.ButtonHovered);
        var colBtnActive = ImGui.GetColorU32(ImGuiCol.ButtonActive);

        bool changed = false;
        for (int y = 0; y < rows; y++)
        {
            float yPos = pos.Y + y * sizeCard.Y;
            for (int x = 0; x < cols; x++)
            {
                float xPos = pos.X + x * sizeCard.X;
                Vector2 min = new(xPos, yPos);
                Vector2 max = min + sizeCard;

                var year = pageStart + y * cols + x + 1;
                if (year > DateTime.MaxValue.Year)
                {
                    break;
                }

                ImRect hitBox = new(min, max);
                var colCard = colBtn;

                if (year == time.Year)
                {
                    colCard = ImGui.GetColorU32(ImGuiCol.HeaderActive);
                }

                var sb = TextHelper.Builder;
                FormatYear(ref sb, year);
                sb.End();

                using ImGuiStyleColScope colScope = new(ImGuiCol.Button, colCard);
                if (ImGuiButton.InlineButton(sb, hitBox, new(0.5f), InlineButtonFlags.NoRounding | InlineButtonFlags.FillSpace))
                {
                    var dim = DateTime.DaysInMonth(year, time.Month);
                    time = new DateTime(year, time.Month, Math.Min(dim, time.Day));
                    state = new(time, time);
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static void FormatYear(ref StrBuilder builder, int year)
    {
        if (year < 10)
        {
            builder.Append("000"u8);
        }
        else if (year < 100)
        {
            builder.Append("00"u8);
        }
        else if (year < 1000)
        {
            builder.Append("0"u8);
        }
        builder.Append(year);
    }
}