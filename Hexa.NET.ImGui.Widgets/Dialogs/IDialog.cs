namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using System.Numerics;

    public interface IDialog
    {
        bool Shown { get; }

        Vector2 WindowPos { get; }

        Vector2 WindowSize { get; }

        void Draw();

        void Close();

        void Reset();

        void Show();
    }
}