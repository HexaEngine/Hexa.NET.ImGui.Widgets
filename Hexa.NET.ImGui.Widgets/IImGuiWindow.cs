namespace Hexa.NET.ImGui.Widgets
{
    public interface IImGuiWindow
    {
        bool IsEmbedded { get; internal set; }

        void Close();

        void Dispose();

        void DrawContent();

        void DrawMenu();

        void DrawWindow(ImGuiWindowFlags overwriteFlags);

        void Init();

        void Show();
    }
}