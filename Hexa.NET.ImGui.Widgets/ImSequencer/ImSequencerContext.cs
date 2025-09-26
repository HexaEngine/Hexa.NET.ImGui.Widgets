using System.Numerics;

namespace Hexa.NET.ImGui.Widgets.ImSequencer
{
    public struct ImSequencerContext
    {
        public float FramePixelWidth = 10.0f;
        public float FramePixelWidthTarget = 10.0f;
        public int MovingEntry = -1;
        public int MovingPos = -1;
        public int MovingPart = -1;

        public bool MovingScrollBar = false;
        public bool MovingCurrentFrame = false;

        public bool PanningView = false;
        public Vector2 PanningViewSource;
        public int PanningViewFrame;

        public float CursorWidth = 8.0f;

        public bool SizingRBar = false;
        public bool SizingLBar = false;
        public float MinBarWidth = 44.0f;

        public ImSequencerContext()
        {
        }
    }
}