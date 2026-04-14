namespace Hexa.NET.ImGui.Widgets
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Utilities.Text;

    /// <summary>
    /// A text helper for ImGui.
    /// </summary>
    public unsafe static class TextHelper
    {
        private static byte* buffer;
        private static int capacity;

        static TextHelper()
        {
            buffer = AllocT<byte>((nuint)4096);
            capacity = 4096;
        }

        public static void Release()
        {
            if (buffer == null)
                return;
            Free(buffer);
            buffer = null;
            capacity = 0;
        }

        public static void Resize(int newCapacity)
        {
            buffer = ReAllocT(buffer, newCapacity);
            capacity = newCapacity;
        }

        /// <summary>
        /// Gets a shared <see cref="StrBuilder"/> instance backed by the internal buffer.
        /// The instance is shared and should be used one-shot for string interpolation building only.
        /// Do not store or reuse the returned instance across calls.
        /// This property is not thread-safe, but since ImGui is single-threaded this is not a concern.
        /// </summary>
        public static StrBuilder Builder => new(buffer, capacity);

        /// <summary>
        /// Centers a text vertically.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void TextCenteredV(string text)
        {
            var windowHeight = ImGui.GetWindowSize().Y;
            var textHeight = ImGui.CalcTextSize(text).Y;

            ImGui.SetCursorPosY((windowHeight - textHeight) * 0.5f);
            ImGui.Text(text);
        }

        /// <summary>
        /// Centers a text horizontally.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void TextCenteredH(string text)
        {
            var windowWidth = ImGui.GetWindowSize().X;
            var textWidth = ImGui.CalcTextSize(text).X;

            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.Text(text);
        }

        /// <summary>
        /// Centers a text vertically and horizontally.
        /// </summary>
        /// <param name="text">The text.</param>
        public static void TextCenteredVH(string text)
        {
            var windowSize = ImGui.GetWindowSize();
            var textSize = ImGui.CalcTextSize(text);

            ImGui.SetCursorPos((windowSize - textSize) * 0.5f);
            ImGui.Text(text);
        }
    }
}