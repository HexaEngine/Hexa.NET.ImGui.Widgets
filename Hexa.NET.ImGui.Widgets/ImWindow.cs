namespace Hexa.NET.ImGui.Widgets
{
    using Hexa.NET.ImGui;

    public abstract class ImWindow : IImGuiWindow
    {
        protected bool IsShown;
        protected bool IsDocked;
        protected bool windowEnded;

        protected abstract string Name { get; }
        protected ImGuiWindowFlags Flags;

        protected bool IsEmbedded;

        public virtual void Init()
        {
        }

        public virtual unsafe void DrawWindow()
        {
            if (!IsShown) return;

            if (IsEmbedded)
            {
                ImGuiWindowClass windowClass;
                windowClass.DockNodeFlagsOverrideSet = (ImGuiDockNodeFlags)ImGuiDockNodeFlagsPrivate.NoTabBar;
                ImGui.SetNextWindowClass(&windowClass);
                ImGui.SetNextWindowDockID(WidgetManager.DockSpaceId);
            }

            if (!ImGui.Begin(Name, ref IsShown, Flags))
            {
                ImGui.End();
                return;
            }

            windowEnded = false;

            DrawContent();

            if (!windowEnded)
                ImGui.End();
        }

        public abstract void DrawContent();

        protected void EndWindow()
        {
            if (!IsShown) return;
            ImGui.End();
            windowEnded = true;
        }

        public virtual void DrawMenu()
        {
            if (ImGui.MenuItem(Name))
            {
                IsShown = true;
            }
        }

        public virtual void Show()
        {
            IsShown = true;
        }

        public virtual void Close()
        {
            IsShown = false;
        }

        public virtual void Dispose()
        {
        }
    }
}