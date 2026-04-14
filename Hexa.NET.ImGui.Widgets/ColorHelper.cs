
namespace Hexa.NET.ImGui.Widgets;

using System.Globalization;
using System.Numerics;

public static class ColorHelper
{
    public static float DefaultThreshold = 0.5f;

    public static float Luma(Vector4 color)
    {
        var prep = color * new Vector4(0.299f, 0.587f, 0.114f, 1.0f);
        return prep.X + prep.Y + prep.Z;
    }

    public static Vector4 FixContrast(Vector4 background, Vector4 foreground)
    {
        return FixContrast(background, foreground, DefaultThreshold);
    }

    public static Vector4 FixContrast(Vector4 background, Vector4 foreground, float threshold)
    {
        var fgPremult = foreground * foreground.W;
        var bgPremult = background * background.W;
        var fgLuma = Luma(fgPremult);
        var bgLuma = Luma(bgPremult);

        if (MathF.Abs(fgLuma - bgLuma) < threshold)
        {
            var hsl = RGBToHSL(foreground);
            hsl.Z = 1.0f - hsl.Z; // Invert lightness
            return HSLToRGB(hsl);
        }

        return foreground;
    }

    public static uint FixContrastABGR(uint bg, uint fg)
    {
        return FixContrastABGR(bg, fg, DefaultThreshold);
    }

    public static uint FixContrastABGR(uint bg, uint fg, float threshold)
    {
        var bgColor = ImGui.ColorConvertU32ToFloat4(bg);
        var fgColor = ImGui.ColorConvertU32ToFloat4(fg);
        var fixedColor = FixContrast(bgColor, fgColor, threshold);
        return ImGui.ColorConvertFloat4ToU32(fixedColor);
    }

    public static Vector4 RGBToHSL(Vector4 color)
    {
        float r = color.X;
        float g = color.Y;
        float b = color.Z;
        float a = color.W;

        float max = MathF.Max(r, MathF.Max(g, b));
        float min = MathF.Min(r, MathF.Min(g, b));
        float h = 0f;
        float s = 0f;
        float l = (max + min) * 0.5f;

        float delta = max - min;

        if (delta > 0f)
        {
            s = delta / (1f - MathF.Abs(2f * l - 1f));

            if (max == r)
            {
                h = ((g - b) / delta) % 6f;
            }
            else if (max == g)
            {
                h = ((b - r) / delta) + 2f;
            }
            else
            {
                h = ((r - g) / delta) + 4f;
            }

            h /= 6f;
            if (h < 0f)
                h += 1f;
        }

        return new Vector4(h, s, l, a);
    }

    public static Vector4 HSLToRGB(Vector4 color)
    {
        float h = color.X;
        float s = color.Y;
        float l = color.Z;
        float a = color.W;

        float r, g, b;

        if (s <= 0f)
        {
            r = g = b = l;
        }
        else
        {
            float q = l < 0.5f
                ? l * (1f + s)
                : l + s - l * s;

            float p = 2f * l - q;

            static float HueToRGB(float p, float q, float t)
            {
                if (t < 0f) t += 1f;
                if (t > 1f) t -= 1f;

                if (t < 1f / 6f) return p + (q - p) * 6f * t;
                if (t < 1f / 2f) return q;
                if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
                return p;
            }

            r = HueToRGB(p, q, h + 1f / 3f);
            g = HueToRGB(p, q, h);
            b = HueToRGB(p, q, h - 1f / 3f);
        }

        return new Vector4(r, g, b, a);
    }

    public static uint GetCurrentWindowBg()
    {
        var window = ImGuiP.GetCurrentWindow();
        var isPopup = (window.Flags & ImGuiWindowFlags.Popup) != 0;
        var winBg = isPopup ? ImGui.GetColorU32(ImGuiCol.PopupBg) : ImGui.GetColorU32(ImGuiCol.WindowBg);
        return winBg;
    }
}
