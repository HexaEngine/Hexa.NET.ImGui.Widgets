namespace Hexa.NET.ImGui.Widgets.ImSequencer
{
    public struct ImSequencerCustomDraw
    {
        public int Index;
        public ImRect CustomRect;
        public ImRect LegendRect;
        public ImRect ClippingRect;
        public ImRect LegendClippingRect;

        public ImSequencerCustomDraw(int index, ImRect customRect, ImRect legendRect, ImRect clippingRect, ImRect legendClippingRect)
        {
            Index = index;
            CustomRect = customRect;
            LegendRect = legendRect;
            ClippingRect = clippingRect;
            LegendClippingRect = legendClippingRect;
        }
    };
}