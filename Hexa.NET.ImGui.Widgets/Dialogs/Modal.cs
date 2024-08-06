namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using Hexa.NET.ImGui;
    using System.Runtime.CompilerServices;

    public abstract class Modal
    {
        private bool windowEnded;
        private bool signalShow;
        private bool signalClose;

        public abstract string Name { get; }

        protected abstract ImGuiWindowFlags Flags { get; }

        public unsafe void Draw()
        {
            if (signalShow)
            {
                ImGui.OpenPopup(Name);
                signalShow = false;
            }
            if (!ImGui.BeginPopupModal(Name, null, Flags))
            {
                return;
            }
            if (signalClose)
            {
                ImGui.CloseCurrentPopup();
                signalClose = false;
            }
            windowEnded = false;

            DrawContent();

            if (!windowEnded)
                ImGui.EndPopup();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EndDraw()
        {
            ImGui.EndPopup();
            windowEnded = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void DrawContent();

        public virtual void Hide()
        {
            signalClose = true;
        }

        public abstract void Reset();

        public virtual void Show()
        {
            signalShow = true;
        }
    }
}