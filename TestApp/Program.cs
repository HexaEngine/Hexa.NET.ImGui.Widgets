namespace TestApp
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImPlot;
    using Hexa.NET.OpenGL;
    using Hexa.NET.SDL2;
    using System.Numerics;

    public unsafe class Program
    {
        private static bool exiting = false;
        private static readonly List<Func<SDLEvent, bool>> hooks = new();
        private static SDLWindow* mainWindow;
        private static uint mainWindowId;

        private static int width;
        private static int height;

        private static ImGuiManager imGuiManager;
        internal static GL GL;
        private static SDLGLContext glcontext;

        public static int Width => width;

        public static int Height => height;

        public static event EventHandler<ResizedEventArgs>? Resized;

        private static void Main(string[] args)
        {
            SDL.SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            SDL.SetHint(SDL.SDL_HINT_MOUSE_AUTO_CAPTURE, "0");
            SDL.SetHint(SDL.SDL_HINT_AUTO_UPDATE_JOYSTICKS, "1");
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4, "1");
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4_RUMBLE, "1");
            SDL.SetHint(SDL.SDL_HINT_JOYSTICK_RAWINPUT, "0");
            SDL.Init(SDL.SDL_INIT_EVERYTHING);

            SDL.GLSetAttribute(SDLGLattr.GlContextMajorVersion, 3);
            SDL.GLSetAttribute(SDLGLattr.GlContextMinorVersion, 3);
            SDL.GLSetAttribute(SDLGLattr.GlContextProfileMask, (int)SDLGLprofile.GlContextProfileCore);

            int width = 1280;
            int height = 720;
            int y = 100;
            int x = 100;

            SDLWindowFlags flags = SDLWindowFlags.Resizable | SDLWindowFlags.Hidden | SDLWindowFlags.AllowHighdpi | SDLWindowFlags.Opengl;
            mainWindow = SDL.CreateWindow("", x, y, width, height, (uint)flags);
            mainWindowId = SDL.GetWindowID(mainWindow);

            InitGraphics(mainWindow);
            InitImGui(mainWindow);

            SDL.ShowWindow(mainWindow);

            Time.Initialize();

            SDLEvent evnt;
            while (!exiting)
            {
                SDL.PumpEvents();
                while (SDL.PollEvent(&evnt) == (int)SDLBool.True)
                {
                    for (int i = 0; i < hooks.Count; i++)
                    {
                        hooks[i](evnt);
                    }

                    HandleEvent(evnt);
                }

                Render();

                Time.FrameUpdate();
            }

            WidgetManager.Dispose();
            imGuiManager.Dispose();

            SDL.DestroyWindow(mainWindow);

            SDL.Quit();
        }

        private static void Render()
        {
            imGuiManager.NewFrame();

            WidgetManager.Draw();
            ImGui.ShowDemoWindow();

            SDL.GLMakeCurrent(mainWindow, glcontext);
            GL.BindFramebuffer(GLFramebufferTarget.Framebuffer, 0);
            GL.Clear(GLClearBufferMask.ColorBufferBit | GLClearBufferMask.DepthBufferBit);

            ImGuiManager.EndFrame();

            SDL.GLMakeCurrent(mainWindow, glcontext);
            SDL.GLSwapWindow(mainWindow);
        }

        private static void InitGraphics(SDLWindow* mainWindow)
        {
            SDLNativeContext context = new(mainWindow);
            glcontext = context.Handle;
            GL = new(context);
        }

        private static void InitImGui(SDLWindow* mainWindow)
        {
            imGuiManager = new(mainWindow, glcontext);
            WidgetDemo demo = new();
            demo.Show();

            WidgetDemo2 demo2 = new();
            demo2.Show();

            WidgetManager.Init();
        }

        private static void Resize(int width, int height, int oldWidth, int oldHeight)
        {
            GL.Viewport(0, 0, width, height);
            Resized?.Invoke(null, new(width, height, oldWidth, oldHeight));
        }

        public static void RegisterHook(Func<SDLEvent, bool> hook)
        {
            hooks.Add(hook);
        }

        private static void HandleEvent(SDLEvent evnt)
        {
            SDLEventType type = (SDLEventType)evnt.Type;
            switch (type)
            {
                case SDLEventType.Windowevent:
                    {
                        var even = evnt.Window;
                        if (even.WindowID == mainWindowId)
                        {
                            switch ((SDLWindowEventID)evnt.Window.Event)
                            {
                                case SDLWindowEventID.Close:
                                    exiting = true;
                                    break;

                                case SDLWindowEventID.Resized:
                                    int oldWidth = Program.width;
                                    int oldHeight = Program.height;
                                    int width = even.Data1;
                                    int height = even.Data2;
                                    Resize(width, height, oldWidth, oldHeight);
                                    Program.width = width;
                                    Program.height = height;
                                    break;
                            }
                        }
                    }
                    break;

                case SDLEventType.Dropfile:
                    SDL.Free(evnt.Drop.File);
                    break;
            }
        }
    }
}