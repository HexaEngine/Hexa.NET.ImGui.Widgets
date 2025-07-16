namespace TestApp
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Backends.OpenGL3;
    using Hexa.NET.ImGui.Backends.SDL2;
    using Hexa.NET.ImGui.Utilities;
    using Hexa.NET.ImPlot;
    using Hexa.NET.SDL2;
    using Hexa.NET.Utilities;
    using System;
    using System.Diagnostics;

    public class ImGuiManager
    {
        private ImGuiContextPtr guiContext;
        private ImPlotContextPtr plotContext;

        private enum ImGuiFreeTypeBuilderFlags
        {
            NoHinting = 1 << 0,   // Disable hinting. This generally generates 'blurrier' bitmap glyphs when the glyph are rendered in any of the anti-aliased modes.
            NoAutoHint = 1 << 1,   // Disable auto-hinter.
            ForceAutoHint = 1 << 2,   // Indicates that the auto-hinter is preferred over the font's native hinter.
            LightHinting = 1 << 3,   // A lighter hinting algorithm for gray-level modes. Many generated glyphs are fuzzier but better resemble their original shape. This is achieved by snapping glyphs to the pixel grid only vertically (Y-axis), as is done by Microsoft's ClearType and Adobe's proprietary font renderer. This preserves inter-glyph spacing in horizontal text.
            MonoHinting = 1 << 4,   // Strong hinting algorithm that should only be used for monochrome output.
            Bold = 1 << 5,   // Styling: Should we artificially embolden the font?
            Oblique = 1 << 6,   // Styling: Should we slant the font, emulating italic style?
            Monochrome = 1 << 7,   // Disable anti-aliasing. Combine this with MonoHinting for best results!
            LoadColor = 1 << 8,   // Enable FreeType color-layered glyphs
            Bitmap = 1 << 9    // Enable FreeType bitmap glyphs
        };

        public unsafe ImGuiManager(Hexa.NET.SDL2.SDLWindow* window, SDLGLContext context)
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
            io.ConfigDebugIsDebuggerPresent = Debugger.IsAttached;
            io.ConfigErrorRecoveryEnableAssert = true;

            // Setup Platform
            ImGuiImplSDL2.SetCurrentContext(guiContext);
            ImGuiImplSDL2.InitForOpenGL((Hexa.NET.ImGui.Backends.SDL2.SDLWindow*)window, (void*)context.Handle);
            Program.RegisterHook(HookCallback);

            ImGuiFontBuilder builder = new(ImGui.GetIO().Fonts);

            var range = io.Fonts.GetGlyphRangesDefault();
            int end = 0;
            while (range[end] != 0) end++;

            var array = Utils.ToManaged(range, end)!;
            Array.Resize(ref array, array.Length + 2);
            array[^2] = 0x2200;
            array[^1] = 0x22FF;

            builder.SetOption(config => { config.PixelSnapH = true; config.OversampleH = 2; config.OversampleV = 2; });
            builder.AddFontFromFileTTF("assets/fonts/arialuni.ttf", 18, array);
            builder.SetOption(config =>
            {
                config.GlyphMinAdvanceX = 18;
                config.GlyphOffset = new(0, 4);
                config.MergeMode = true;
            });
            builder.AddFontFromFileTTF("assets/fonts/MaterialSymbolsRounded.ttf", 16.0f, [0xe003, 0xF8FF])
            .Build();

            // Setup Renderer
            ImGuiImplOpenGL3.SetCurrentContext(guiContext);
            ImGuiImplOpenGL3.Init((byte*)null);

            ImPlot.SetImGuiContext(guiContext);
            plotContext = ImPlot.CreateContext();
            ImPlot.SetCurrentContext(plotContext);
            ImPlot.StyleColorsDark(ImPlot.GetStyle());
        }

        private static unsafe bool HookCallback(Hexa.NET.SDL2.SDLEvent @event)
        {
            return ImGuiImplSDL2.ProcessEvent((Hexa.NET.ImGui.Backends.SDL2.SDLEvent*)&@event);
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