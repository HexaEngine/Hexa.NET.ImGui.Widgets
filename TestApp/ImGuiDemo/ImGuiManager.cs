namespace TestApp.ImGuiDemo
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Backends.OpenGL3;
    using Hexa.NET.ImGui.Backends.SDL2;
    using Silk.NET.OpenGL;
    using Silk.NET.SDL;
    using System;
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
            ImGuiImplSDL2.SetCurrentContext(guiContext);
            ImGuiImplSDL2.InitForOpenGL((SDLWindow*)window, (void*)context.Handle);
            TestApp.Program.RegisterHook(HookCallback);

            // setup fonts.
            var config = ImGui.ImFontConfig();
            //io.Fonts.AddFontDefault(config);
            config.PixelSnapH = true;
            config.OversampleH = 2;
            config.OversampleV = 2;

            io.Fonts.AddFontFromFileTTF("assets/fonts/arialuni.ttf", 18, config, io.Fonts.GetGlyphRangesChineseFull());

            // load custom font

            config.MergeMode = true;
            config.GlyphMinAdvanceX = 18;
            config.GlyphOffset = new(0, 4);
            char* range = stackalloc char[] { (char)0xe003, (char)0xF8FF, (char)0 };

            // IMPORTANT: AddFontFromMemoryTTF() by default transfer ownership of the data buffer to the font atlas, which will attempt to free it on destruction.
            // This was to avoid an unnecessary copy, and is perhaps not a good API (a future version will redesign it).
            // Set config.FontDataOwnedByAtlas to false to keep ownership of the data (so you need to free the data yourself).
            io.Fonts.AddFontFromFileTTF("assets/fonts/MaterialSymbolsRounded.ttf", 18, config, range);

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
            ImGuiImplOpenGL3.SetCurrentContext(guiContext);
            ImGuiImplOpenGL3.Init((byte*)null);
        }

        private static unsafe bool HookCallback(Event @event)
        {
            return ImGuiImplSDL2.ProcessEvent((SDLEvent*)&@event);
        }

        public unsafe void NewFrame()
        {
            // Set ImGui context
            ImGui.SetCurrentContext(guiContext);

            // Start new frame, call order matters.
            ImGuiImplOpenGL3.NewFrame();
            ImGuiImplSDL2.NewFrame();
            ImGui.NewFrame();
        }

        public static unsafe void EndFrame()
        {
            // Renders ImGui Data
            var io = ImGui.GetIO();
            ImGui.Render();
            ImGui.EndFrame();
            ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

            // Update and Render additional Platform Windows
            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
            }
        }

        public void Dispose()
        {
            ImGuiImplOpenGL3.Shutdown();
            ImGuiImplSDL2.Shutdown();
        }
    }
}