namespace TestApp.ImGuiDemo
{
    using Hexa.NET.ImGui;
    using Silk.NET.OpenGL;
    using Silk.NET.SDL;
    using TestApp;

    public class ImGuiManager
    {
        private ImGuiContextPtr guiContext;

        public unsafe ImGuiManager(Window* window, GL gl, SDLGLContext context)
        {
            // Create ImGui context
            guiContext = ImGui.CreateContext(null);

            // Set ImGui context
            ImGui.SetCurrentContext(guiContext);

            // Setup ImGui config.
            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;     // Enable Keyboard Controls
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;      // Enable Gamepad Controls
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;         // Enable Docking
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;       // Enable Multi-Viewport / Platform Windows
            io.ConfigViewportsNoAutoMerge = false;
            io.ConfigViewportsNoTaskBarIcon = false;

            // Setup Platform
            ImGuiSDL2Platform.InitForOpenGL(window, (void*)context.Handle);

            // setup fonts.
            var config = ImGui.ImFontConfig();
            //io.Fonts.AddFontDefault(config);
            config.PixelSnapH = true;
            config.OversampleH = 2;
            config.OversampleV = 2;

            io.Fonts.AddFontFromFileTTF("assets/fonts/arialuni.TTF", 20, config, io.Fonts.GetGlyphRangesChineseFull());

            // load custom font

            config.MergeMode = true;
            config.GlyphMinAdvanceX = 18;
            config.GlyphOffset = new(0, 4);
            char* range = stackalloc char[] { (char)0xe003, (char)0xF8FF, (char)0 };

            // IMPORTANT: AddFontFromMemoryTTF() by default transfer ownership of the data buffer to the font atlas, which will attempt to free it on destruction.
            // This was to avoid an unnecessary copy, and is perhaps not a good API (a future version will redesign it).
            // Set config.FontDataOwnedByAtlas to false to keep ownership of the data (so you need to free the data yourself).
            io.Fonts.AddFontFromFileTTF("assets/fonts/MaterialSymbolsRounded.TTF", 14, config, range);

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

            // Setup Renderer
            ImGuiOpenGL3Renderer.Init(gl, null);
        }

        public unsafe void NewFrame()
        {
            // Set ImGui context
            ImGui.SetCurrentContext(guiContext);

            // Start new frame, call order matters.
            ImGuiSDL2Platform.NewFrame();
            ImGui.NewFrame();
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