namespace Hexa.NET.ImGui.Widgets.Extensions
{
    using System.Numerics;

    public static unsafe class ImRectExtensions
    {
        public static bool Contains(this in ImRect self, in Vector2 pos)
        {
            return pos.X >= self.Min.X && pos.X < self.Max.X && pos.Y >= self.Min.Y && pos.Y < self.Max.Y;
        }

        public static ImRect Expand(this in ImRect self, float scale)
        {
            Vector2 halfScale = new(scale, scale);
            return new ImRect(self.Min - halfScale, self.Max + halfScale);
        }
    }
}
