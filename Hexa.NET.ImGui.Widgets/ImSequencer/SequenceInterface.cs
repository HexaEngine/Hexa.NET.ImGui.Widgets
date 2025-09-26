namespace Hexa.NET.ImGui.Widgets.ImSequencer
{
    using Hexa.NET.Utilities.Text;

    public abstract unsafe class SequenceInterface
    {
        public bool focused = false;

        public abstract int GetFrameMin();

        public abstract int GetFrameMax();

        public abstract int GetItemCount();

        public virtual void BeginEdit(int index)
        {
        }

        public virtual void EndEdit()
        {
        }

        public virtual int GetItemTypeCount()
        {
            return 0;
        }

        public virtual ReadOnlySpan<byte> GetItemTypeName(int typeIndex)
        {
            return ""u8;
        }

        public virtual ReadOnlySpan<byte> GetItemLabel(int index)
        {
            return ""u8;
        }

        public virtual void FormatCollapse(ref StrBuilder builder, int frameCount, int sequenceCount)
        {
            builder.Append(frameCount);
            builder.Append(" Frames / "u8);
            builder.Append(sequenceCount);
            builder.Append(" entries"u8);
        }

        public abstract void Get(int index, int** start, int** end, int* type, uint* color);

        public virtual void Add(int type)
        {
        }

        public virtual void Del(int index)
        {
        }

        public virtual void Duplicate(int index)
        {
        }

        public virtual void Copy()
        {
        }

        public virtual void Paste()
        {
        }

        public virtual nuint GetCustomHeight(int index)
        {
            return 0;
        }

        public virtual void DoubleClick(int index)
        {
        }

        public virtual void CustomDraw(int index, ImDrawList* draw_list, ref ImRect rc, ref ImRect legendRect, ref ImRect clippingRect, ref ImRect legendClippingRect)
        {
        }

        public virtual void CustomDrawCompact(int index, ImDrawList* draw_list, ref ImRect rc, ref ImRect clippingRect)
        {
        }
    }
}