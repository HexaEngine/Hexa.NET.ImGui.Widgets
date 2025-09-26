namespace Hexa.NET.ImGui.Widgets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class ImMath
    {
        public static float Clamp(float value, float min, float max)
        {
#if NET5_0_OR_GREATER
            return Math.Clamp(value, min, max);
#else
            return Math.Max(min, Math.Min(max, value));
#endif
        }

        public static int Clamp(int value, int min, int max)
        {
#if NET5_0_OR_GREATER
            return Math.Clamp(value, min, max);
#else
            return Math.Max(min, Math.Min(max, value));
#endif
        }

        public static float Lerp(float x, float y, float v)
        {
            return x + (y - x) * v;
        }

        public static float Log2(float x)
        {
#if NET5_0_OR_GREATER
            return MathF.Log2(x);
#else
            return MathF.Log(x, 2);
#endif
        }

        public const float FloatEpsilon = 1.192092896e-07F;
    }
}