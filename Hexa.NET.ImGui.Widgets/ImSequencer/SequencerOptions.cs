namespace Hexa.NET.ImGui.Widgets.ImSequencer
{
    public enum SequencerOptions
    {
        EditNone = 0,
        EditStartend = 1 << 1,
        ChangeFrame = 1 << 3,
        Add = 1 << 4,
        Del = 1 << 5,
        Copypaste = 1 << 6,
        EditAll = EditStartend | ChangeFrame
    };
}