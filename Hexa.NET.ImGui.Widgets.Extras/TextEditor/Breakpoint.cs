namespace Hexa.NET.ImGui.Widgets.Extras.TextEditor
{
    using System;

    public struct Breakpoint : IEquatable<Breakpoint>
    {
        public int Line;
        public bool Enabled;

        public Breakpoint(int line, bool enabled)
        {
            Line = line;
            Enabled = enabled;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is Breakpoint breakpoint && Equals(breakpoint);
        }

        public readonly bool Equals(Breakpoint other)
        {
            return Line == other.Line &&
                   Enabled == other.Enabled;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Line, Enabled);
        }

        public static bool operator ==(Breakpoint left, Breakpoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Breakpoint left, Breakpoint right)
        {
            return !(left == right);
        }
    }
}