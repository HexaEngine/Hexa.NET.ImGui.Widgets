namespace Hexa.NET.ImGui.Widgets
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using System.Numerics;

    public static class WidgetManager
    {
        private static bool initialized;
        private static readonly List<IImGuiWindow> widgets = new();

        static WidgetManager()
        {
        }

        public static bool BlockInput { get; set; }

        public static uint DockSpaceId { get; private set; }

        public static WidgetStyle Style { get; set; } = new();

        public static IReadOnlyList<IImGuiWindow> Widgets => widgets;

        public static bool Register<T>(bool show = false, bool mainWindow = false) where T : IImGuiWindow, new()
        {
            return Register(new T(), show, mainWindow);
        }

        public static void Unregister<T>() where T : IImGuiWindow, new()
        {
            IImGuiWindow? window = widgets.FirstOrDefault(x => x is T);
            if (window != null)
            {
                if (initialized)
                {
                    window.Dispose();
                }

                widgets.Remove(window);
            }
        }

        public static bool Register(IImGuiWindow widget, bool show = false, bool mainWindow = false)
        {
            if (show)
            {
                widget.Show();
            }

            if (widgets.Count == 0)
            {
                widget.IsEmbedded = true;
            }

            if (mainWindow)
            {
                widget.IsEmbedded = true;
                for (int i = 0; i < widgets.Count; i++)
                {
                    widgets[i].IsEmbedded = false;
                }
            }

            if (!initialized)
            {
                widgets.Add(widget);
                return false;
            }
            else
            {
                widget.Init();
                widgets.Add(widget);
                return true;
            }
        }

        public static void Init()
        {
            for (int i = 0; i < widgets.Count; i++)
            {
                var widget = widgets[i];
                widget.Init();
            }
            ImGuiGC.Init();
            initialized = true;
        }

        public static void Draw()
        {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
            DockSpaceId = ImGui.DockSpaceOverViewport(null, ImGuiDockNodeFlags.PassthruCentralNode, null); // passing null as first argument will use the main viewport
            ImGui.PopStyleColor(1);

            ImGui.BeginDisabled(BlockInput);

            ImGuiWindowFlags overwriteFlags = ImGuiWindowFlags.None;
            if (BlockInput)
            {
                overwriteFlags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBringToFrontOnFocus;
            }

            for (int i = 0; i < widgets.Count; i++)
            {
                widgets[i].DrawWindow(overwriteFlags);
            }

            ImGui.EndDisabled();

            DialogManager.Draw();
            MessageBoxes.Draw();
            AnimationManager.Tick();
        }

        public static unsafe void DrawMenu()
        {
            for (int i = 0; i < widgets.Count; i++)
            {
                widgets[i].DrawMenu();
            }
        }

        public static void Dispose()
        {
            for (int i = 0; i < widgets.Count; i++)
            {
                widgets[i].Dispose();
            }
            widgets.Clear();
            ImGuiGC.Shutdown();
            initialized = false;
        }
    }
}