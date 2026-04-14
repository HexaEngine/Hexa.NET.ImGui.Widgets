
namespace Hexa.NET.ImGui.Widgets;

using Hexa.NET.ImGui.Widgets.Text;
using Hexa.NET.Utilities.Text;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;

public unsafe static class DatePicker
{
    public static void Draw(ReadOnlySpan<byte> label, ref DateTime time)
    {
        fixed (byte* ptr = label)
        {
            Draw(ptr, ref time);
        }
    }

    public static void Draw(byte* label, ref DateTime time)
    {
        var sb = TextHelper.Builder;
        sb.Append(label);
        sb.Append("##"u8);
        sb.Append(label);
        sb.Append("+input"u8);
        sb.End();

        const int bufSize = 64;
        byte* buf = stackalloc byte[bufSize];
        StrBuilder sb2 = new(buf, bufSize);
        sb2.Append(time, CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
        sb2.End();

        ImGui.SetNextItemWidth(100);
        ImGui.InputText(sb, sb2, (nuint)sb2.Count, ImGuiInputTextFlags.ReadOnly);

        sb.Reset();
        sb.Append("##"u8);
        sb.Append(label);
        sb.Append("+popup"u8);
        sb.End();
        if (ImGui.IsItemClicked())
        {
            ImGui.OpenPopup(sb);
        }

        if (ImGui.BeginPopup(sb))
        {
            DrawDatePicker(label, ref time);
            ImGui.EndPopup();
        }
    }

    public readonly struct CompressedYearMonth : IEquatable<CompressedYearMonth>
    {
        private readonly uint value;

        public const uint MaxValue = 9998 * 12 + 11;

        public CompressedYearMonth(DateTime time)
        {
            value = (uint)((time.Year - 1) * 12 + (time.Month - 1));
        }

        public CompressedYearMonth(uint value)
        {
            this.value = value;
        }

        public readonly uint Value => value;

        public readonly int Month => (int)(value % 12) + 1;

        public readonly int Year => (int)(value / 12) + 1;

        public readonly DateTime ToDateTime()
        {
            return new(Year, Month, 1);
        }

        public readonly override int GetHashCode()
        {
            var x = value;
            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return (int)x;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is CompressedYearMonth month && Equals(month);
        }

        public readonly bool Equals(CompressedYearMonth other)
        {
            return value == other.value;
        }

        public static bool operator ==(CompressedYearMonth left, CompressedYearMonth right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CompressedYearMonth left, CompressedYearMonth right)
        {
            return !(left == right);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public readonly struct DatePickerState
    {
        // DO NOT CHANGE THE SIZE OF THIS STRUCT WITHOUT CHANGING THE MAKEVALUE METHOD,
        // IT RELIES ON BEING EXACTLY 4 BYTES TO WORK PROPERLY.
        private readonly uint value;

        private const uint DateBits = 17;
        private const uint HashBits = 32 - DateBits;
        private const uint DateMask = (1u << (int)DateBits) - 1;
        private const uint HashMask = (1u << (int)HashBits) - 1;

        public DatePickerState(DateTime editState, DateTime userState)
        {
            value = MakeValue(editState, userState);
        }

        public DatePickerState(DateTime time) : this(time, time) { }

        public readonly DateTime EditState => new CompressedYearMonth(value & DateMask).ToDateTime();

        public readonly int UserHash => (int)((value >> (int)DateBits) & HashMask);

        private static uint MakeValue(DateTime editState, DateTime userState)
        {
            CompressedYearMonth editYM = new(editState);
            CompressedYearMonth userYM = new(userState);
            var hash = (uint)userYM.GetHashCode();
            return (hash & HashMask) << (int)DateBits | (editYM.Value & DateMask);
        }

        public readonly bool CompareUserState(DateTime userState)
        {
            CompressedYearMonth userYM = new(userState);
            var hash = (uint)userYM.GetHashCode();
            return (hash & HashMask) == UserHash;
        }
    }

    private static bool DrawDatePicker(byte* label, ref DateTime dateTime)
    {
        var window = ImGuiP.GetCurrentWindow();
        if (window.SkipItems) return false;

        var style = ImGui.GetStyle();
        var id = ImGui.GetID(label);

        // Looks weird but it's safe, because DatePickerState is internally just a uint.
        ref var state = ref *(DatePickerState*)ImGui.GetStateStorage().GetIntRef(id, 0);
        if (!state.CompareUserState(dateTime))
        {
            state = new(dateTime);
        }

        DateTime editDate = state.EditState;
        var days = DateTime.DaysInMonth(editDate.Year, editDate.Month);

        var culture = CultureInfo.CurrentCulture;
        var dateTimeInfo = culture.DateTimeFormat;
        const int daysPerWeek = 7;

        var buttonSize = ImGuiButton.CalcInlineButtonSize(">"u8);
        var cellSize = ImGui.CalcTextSize("00"u8) + style.ItemSpacing * 2;

        DateTime firstOfMonth = new(editDate.Year, editDate.Month, 1);
        var firstDayOfWeek = (int)dateTimeInfo.FirstDayOfWeek;
        var dayOfWeek = ((int)firstOfMonth.DayOfWeek - firstDayOfWeek + daysPerWeek) % daysPerWeek;
        var dayState = dateTime.Date.Day;
        var weeks = (days + dayOfWeek + daysPerWeek - 1) / daysPerWeek;

        float maxY = (weeks + 1) * cellSize.Y;
        float maxX = daysPerWeek * cellSize.X;

        var textSize = ImGui.GetTextLineHeight();
        var innerSpacing = style.ItemInnerSpacing;
        var spacing = style.ItemSpacing;
        var fg = ImGui.GetColorU32(ImGuiCol.Text);

        maxY += Math.Max(textSize, buttonSize.Y) * 2; // for month and year text
        maxY += innerSpacing.Y;
        Vector2 size = new(maxX, maxY);

        var draw = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();

        ImRect rect = new(pos, pos + size);
        ImGuiP.ItemSize(rect);
        if (!ImGuiP.ItemAdd(rect, id, &rect, (ImGuiItemFlags)ImGuiItemFlagsPrivate.AllowOverlap))
        {
            return false;
        }

        byte* buf = stackalloc byte[1024];
        StrBuilder sb2 = new(buf, 1024);

        using ImGuiIDScope idScope = new(label);

        bool changed = false;

        var hovering = ImGuiP.ItemHoverable(rect, id, (ImGuiItemFlags)ImGuiItemFlagsPrivate.AllowOverlap);
        var mousePos = ImGui.GetMousePos();
        var mouseDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var mouseClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        var sb = TextHelper.Builder;
        sb.Append(editDate.Year);
        sb.Append("##"u8);
        sb.Append(label);
        sb.Append("+yeartitle"u8);
        sb.End();
        var sizeYear = ImGui.CalcTextSize(sb);
        Vector2 yearOrigin = new(pos.X + (maxX - sizeYear.X) / 2, pos.Y);

        sb2.Reset();
        sb2.Append("##"u8);
        sb2.Append(label);
        sb2.Append("+yearpopup"u8);
        sb2.End();

        using (ImGuiStyleColScope colScope = new())
        {
            colScope.Push(ImGuiCol.Button, 0);
            if (ImGuiButton.InlineButton(sb, new ImRect(yearOrigin, yearOrigin + sizeYear), new(0.5f), InlineButtonFlags.NoRounding | InlineButtonFlags.FillSpace))
            {
                ImGui.OpenPopup(sb2);
            }
        }

        if (ImGui.BeginPopup(sb2))
        {
            DateTime yearPickerTime = editDate;
            if (YearPicker.Draw("##yearpicker"u8, ref yearPickerTime))
            {
                dateTime = yearPickerTime;
                state = new(dateTime);
                editDate = state.EditState;
                changed = true;
            }
            var year = editDate.Year;
            if (ImGui.InputInt("##yearinput"u8, ref year))
            {
                year = ImMath.Clamp(year, 1, DateTime.MaxValue.Year);
                var dim = DateTime.DaysInMonth(year, dateTime.Month);
                dateTime = new(year, dateTime.Month, Math.Min(dim, dateTime.Day));
                state = new(dateTime);
                editDate = state.EditState;
                changed = true;
            }
            ImGui.EndPopup();
        }

        if (ImGuiButton.InlineButton("<"u8, rect, new(0, 0)))
        {
            if (!(editDate.Month == 1 && editDate.Year == 1))
            {
                editDate = editDate.AddMonths(-1);
                state = new(editDate, dateTime);
            }
        }
        if (ImGuiButton.InlineButton(">"u8, rect, new(1, 0)))
        {
            if (!(editDate.Month == 12 && editDate.Year == DateTime.MaxValue.Year))
            {
                editDate = editDate.AddMonths(1);
                state = new(editDate, dateTime);
            }
        }

        pos.Y += MathF.Max(sizeYear.Y, buttonSize.Y);
        rect.Min = pos;
        if (ImGuiButton.InlineButton(sb.BuildLabel(MaterialIcons.CalendarToday), rect, new(0, 0)))
        {
            editDate = DateTime.Today;
            state = new(editDate, dateTime);
        }

        sb.Reset();
        sb.Append(dateTimeInfo.MonthNames[editDate.Month - 1]);
        sb.Append("##"u8);
        sb.Append(label);
        sb.Append("+monthtitle"u8);
        sb.End();
        var sizeMonth = ImGui.CalcTextSize(sb);
        Vector2 monthOrigin = new(pos.X + (maxX - sizeMonth.X) / 2, pos.Y);

        sb2.Reset();
        sb2.Append("##"u8);
        sb2.Append(label);
        sb2.Append("+monthpopup"u8);
        sb2.End();

        using (ImGuiStyleColScope colScope = new())
        {
            colScope.Push(ImGuiCol.Button, 0);
            if (ImGuiButton.InlineButton(sb, new ImRect(monthOrigin, monthOrigin + sizeMonth), new(0.5f), InlineButtonFlags.NoRounding | InlineButtonFlags.FillSpace))
            {
                ImGui.OpenPopup(sb2);
            }
        }

        if (ImGui.BeginPopup(sb2))
        {
            var month = editDate.Month;
            for (int i = 0; i < 12; i++)
            {
                var monthNum = i + 1;
                var name = dateTimeInfo.MonthNames[i];
                if (ImGui.Selectable(name, month == monthNum))
                {
                    var dim = DateTime.DaysInMonth(editDate.Year, monthNum);
                    dateTime = new(editDate.Year, monthNum, Math.Min(dim, dateTime.Day));
                    state = new(dateTime);
                    editDate = state.EditState;
                    changed = true;
                }
            }
            ImGui.EndPopup();
        }

        pos.Y += MathF.Max(sizeMonth.Y, buttonSize.Y);

        var col1 = ImGui.GetColorU32(ImGuiCol.TableRowBg);
        var col2 = ImGui.GetColorU32(ImGuiCol.TableRowBgAlt);
        for (int i = 0; i < daysPerWeek; i++)
        {
            float x = pos.X + i * cellSize.X;
            Vector2 rectMin = new(x, pos.Y);
            Vector2 rectMax = new(x + cellSize.X, pos.Y + cellSize.Y);
            draw.AddRectFilled(rectMin, rectMax, col1);
            var dayIndex = (i + firstDayOfWeek) % daysPerWeek;
            var name = dateTimeInfo.AbbreviatedDayNames[dayIndex];
            draw.AddText(new Vector2(x + spacing.X, pos.Y + spacing.Y), ColorHelper.FixContrastABGR(col1, fg), name);
        }

        draw.AddLine(new Vector2(pos.X, pos.Y + cellSize.Y), new Vector2(pos.X + maxX, pos.Y + cellSize.Y), 0xFF000000, 1.0f);

        var today = DateTime.Today;
        bool isCurrentMonth = editDate.Year == dateTime.Year && editDate.Month == dateTime.Month;
        bool isTodayMonth = today.Year == editDate.Year && today.Month == editDate.Month;
        for (int w = 0; w < weeks; w++)
        {
            float y = pos.Y + (w + 1) * cellSize.Y; // +1 for weekday names
            for (int d = 0; d < daysPerWeek; d++)
            {
                int day = w * daysPerWeek + d + 1 - dayOfWeek;
                if (day <= 0) continue;
                bool isCurrentDay = isCurrentMonth && dayState == day;
                if (day > days) break;
                float x = pos.X + d * cellSize.X;
                Vector2 rectMin = new(x, y);
                Vector2 rectMax = new(x + cellSize.X, y + cellSize.Y);
                var col = d % 2 == 0 ? col1 : col2;

                bool isToday = isTodayMonth && day == today.Day;
                if (isToday)
                {
                    var c = ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.HeaderHovered));
                    c.W = 0.6f;
                    col = ImGui.ColorConvertFloat4ToU32(c);
                }

                if (isCurrentDay)
                {
                    col = ImGui.GetColorU32(ImGuiCol.HeaderActive);
                }

                ImRect hitBox = new(rectMin, rectMax);

                var builder = TextHelper.Builder;
                builder.Append(day);
                builder.End();

                using ImGuiStyleColScope colScope = new(ImGuiCol.Button, col);
                if (ImGuiButton.InlineButton(builder, hitBox, new(0.5f), InlineButtonFlags.NoRounding | InlineButtonFlags.FillSpace))
                {
                    dateTime = new(editDate.Year, editDate.Month, day);
                    state = new(dateTime);
                    editDate = state.EditState;
                    changed = true;
                }
            }
        }

        return changed;
    }
}
