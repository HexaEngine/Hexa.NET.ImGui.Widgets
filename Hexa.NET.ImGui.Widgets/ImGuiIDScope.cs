namespace Hexa.NET.ImGui.Widgets
{
    using System;
    using System.Numerics;

    public struct ImGuiStyleVarScope : IDisposable
    {
        private int count;

        public ImGuiStyleVarScope() { }

        public ImGuiStyleVarScope(ImGuiStyleVar var, float value)
        {
            ImGui.PushStyleVar(var, value);
            count++;
        }

        public ImGuiStyleVarScope(ImGuiStyleVar var, Vector2 value)
        {
            ImGui.PushStyleVar(var, value);
            count++;
        }

        public void Push(ImGuiStyleVar var, float value) 
        {
            ImGui.PushStyleVar(var, value);
            count++;
        }

        public void Push(ImGuiStyleVar var, Vector2 value) 
        {
            ImGui.PushStyleVar(var, value);
            count++;
        }

        public void Pop(int count = 1)
        {
            if (this.count < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            
            ImGui.PopStyleVar(count);
            this.count -= count;
        }

        public void Dispose()
        {
            if (count > 0)
            {
                ImGui.PopStyleVar(count);
                count = 0;
            }
        }
    }

    public struct ImGuiStyleColScope : IDisposable
    {
        private int count;

        public ImGuiStyleColScope()
        {

        }

        public ImGuiStyleColScope(ImGuiCol col, uint value)
        {
            ImGui.PushStyleColor(col, value);
            count++;
        }

        public ImGuiStyleColScope(ImGuiCol col, Vector4 value)
        {
            ImGui.PushStyleColor(col, value);
            count++;
        }

        public void Push(ImGuiCol col, uint value)
        {
            ImGui.PushStyleColor(col, value);
            count++;
        }

        public void Push(ImGuiCol col, Vector4 value)
        {
            ImGui.PushStyleColor(col, value);
            count++;
        }

        public void Pop(int count = 1)
        {
            if (this.count < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            ImGui.PopStyleColor(count);
            this.count -= count;
        }

        public void Dispose()
        {
            if (count > 0)
            {
                ImGui.PopStyleColor(count);
                count = 0;
            }
        }
    }

    public readonly unsafe struct ImGuiIDScope : IDisposable
    {
        public ImGuiIDScope(string id) => ImGui.PushID(id);

        public ImGuiIDScope(ReadOnlySpan<byte> id) => ImGui.PushID(id);

        public ImGuiIDScope(byte* id) => ImGui.PushID(id);

        public readonly void Dispose() => ImGui.PopID();
    }
}
