namespace TestApp.ImGuiDemo
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGuizmo;
    using Hexa.NET.ImNodes;
    using Hexa.NET.ImPlot;
    using Silk.NET.Core.Native;
    using Silk.NET.Direct3D11;
    using Silk.NET.OpenGL;
    using Silk.NET.SDL;
    using System.Numerics;
    using TestApp;

    public class ImGuiManager
    {
        private ImGuiContextPtr guiContext;
        private ImNodesContextPtr nodesContext;
        private ImPlotContextPtr plotContext;

        public unsafe ImGuiManager(Window* window, GL gl, SDLGLContext context)
        {
            // Create ImGui context
            guiContext = ImGui.CreateContext(null);

            // Set ImGui context
            ImGui.SetCurrentContext(guiContext);

            // Set ImGui context for ImGuizmo
            ImGuizmo.SetImGuiContext(guiContext);

            // Set ImGui context for ImPlot
            ImPlot.SetImGuiContext(guiContext);

            // Set ImGui context for ImNodes
            ImNodes.SetImGuiContext(guiContext);

            // Create and set ImNodes context and set style
            nodesContext = ImNodes.CreateContext();
            ImNodes.SetCurrentContext(nodesContext);
            ImNodes.StyleColorsDark(ImNodes.GetStyle());

            // Create and set ImPlot context and set style
            plotContext = ImPlot.CreateContext();
            ImPlot.SetCurrentContext(plotContext);
            ImPlot.StyleColorsDark(ImPlot.GetStyle());

            // Setup ImGui config.
            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;     // Enable Keyboard Controls
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;      // Enable Gamepad Controls
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;         // Enable Docking
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;       // Enable Multi-Viewport / Platform Windows
            io.ConfigViewportsNoAutoMerge = false;
            io.ConfigViewportsNoTaskBarIcon = false;

            // setup fonts.
            var config = ImGui.ImFontConfig();
            io.Fonts.AddFontDefault(config);

            // load custom font
            config.FontDataOwnedByAtlas = false; // Set this option to false to avoid ImGui to delete the data, used with fixed statement.
            config.MergeMode = true;
            config.GlyphMinAdvanceX = 18;
            config.GlyphOffset = new(0, 4);
            var range = new char[] { (char)0xe003, (char)0xF8FF, (char)0 };
            fixed (char* buffer = range)
            {
                var bytes = File.ReadAllBytes("assets/fonts/MaterialSymbolsRounded.TTF");
                fixed (byte* buffer2 = bytes)
                {
                    // IMPORTANT: AddFontFromMemoryTTF() by default transfer ownership of the data buffer to the font atlas, which will attempt to free it on destruction.
                    // This was to avoid an unnecessary copy, and is perhaps not a good API (a future version will redesign it).
                    // Set config.FontDataOwnedByAtlas to false to keep ownership of the data (so you need to free the data yourself).
                    io.Fonts.AddFontFromMemoryTTF(buffer2, bytes.Length, 14, config, buffer);
                }
            }

            io.Fonts.Build();

            // setup ImGui style
            var style = ImGui.GetStyle();
            var colors = style.Colors;

            // When viewports are enabled we tweak WindowRounding/WindowBg so platform windows can look identical to regular ones.
            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                style.WindowRounding = 0.0f;
                style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;
            }

            // Setup Platform
            ImGuiSDL2Platform.InitForOpenGL(window, (void*)context.Handle);

            // Setup Renderer
            ImGuiOpenGL3Renderer.Init(gl, null);
        }

        public unsafe void NewFrame()
        {
            // Set ImGui context
            ImGui.SetCurrentContext(guiContext);
            // Set ImGui context for ImGuizmo
            ImGuizmo.SetImGuiContext(guiContext);
            // Set ImGui context for ImPlot
            ImPlot.SetImGuiContext(guiContext);
            // Set ImGui context for ImNodes
            ImNodes.SetImGuiContext(guiContext);

            // Set ImNodes context
            ImNodes.SetCurrentContext(nodesContext);
            // Set ImPlot context
            ImPlot.SetCurrentContext(plotContext);

            // Start new frame, call order matters.
            ImGuiSDL2Platform.NewFrame();
            ImGui.NewFrame();
            ImGuizmo.BeginFrame(); // mandatory for ImGuizmo
        }

        public unsafe void EndFrame()
        {
            // Renders ImGui Data
            var io = ImGui.GetIO();
            ImGui.Render();
            ImGui.EndFrame();
            ImGuiOpenGL3Renderer.RenderDrawData(ImGui.GetDrawData());

            // Update and Render additional Platform Windows
            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
            }
        }

        public void Dispose()
        {
            ImGuiOpenGL3Renderer.Shutdown();
            ImGuiSDL2Platform.Shutdown();
        }
    }
}